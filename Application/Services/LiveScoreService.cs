using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Application.Options;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class LiveScoreService : ILiveScoreService
{
    /// <summary>
    /// How long after kick-off a match is still worth asking the provider about. Comfortably past
    /// 90 minutes plus stoppages, extra time and penalties; beyond that the provider has long since
    /// said FINISHED (which retires the match from polling on its own) and anything still unresolved
    /// is waiting on an admin, not on the feed.
    /// </summary>
    private const int PollWindowHours = 4;

    /// <summary>
    /// The longest the poller will sleep with nothing in play. Not a correctness bound - it just
    /// means a fixture added, rescheduled or reconfigured today gets noticed within the hour rather
    /// than whenever the next known kick-off happened to be.
    /// </summary>
    private static readonly TimeSpan MaxIdleDelay = TimeSpan.FromHours(1);

    /// <summary>
    /// Floor on the delay, so a kick-off a few seconds away can't turn the poll loop into a spin.
    /// </summary>
    private static readonly TimeSpan MinDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long an admin's own score outranks the provider's. Long enough for the feed to catch up
    /// with someone watching the match, short enough that one hand-entered goal doesn't leave them
    /// hand-entering every goal after it - see <see cref="ShouldAccept"/>.
    /// </summary>
    private static readonly TimeSpan AdminScoreHold = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _dbContext;
    private readonly IExternalMatchDataService _externalMatchDataService;
    private readonly IOptions<FootballDataApiOptions> _options;
    private readonly ILogger<LiveScoreService> _logger;

    public LiveScoreService(
        IApplicationDbContext dbContext,
        IExternalMatchDataService externalMatchDataService,
        IOptions<FootballDataApiOptions> options,
        ILogger<LiveScoreService> logger)
    {
        _dbContext = dbContext;
        _externalMatchDataService = externalMatchDataService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LiveScoreRefreshSummary> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var now = UkClock.Now;
        var matches = await GetMatchesInPlayAsync(now, cancellationToken);

        var summary = new LiveScoreRefreshSummary { MatchesInPlay = matches.Count };
        if (matches.Count == 0)
        {
            return summary;
        }

        // IIS runs two worker processes side by side for a few seconds during an overlapped recycle,
        // each with its own in-process rate limiter, and both would otherwise poll on their own
        // schedule. The stored poll timestamp is the only thing they share, so it's what settles who
        // goes: if anyone polled inside this interval, this pass stands down.
        var pollInterval = TimeSpan.FromSeconds(_options.Value.LiveScorePollSeconds);
        var lastPolled = matches.Max(m => m.LiveScore?.LastPolledDateTime);
        if (lastPolled is not null && now - lastPolled.Value < pollInterval - TimeSpan.FromSeconds(5))
        {
            summary.SkippedReason = "another worker polled inside this interval";
            return summary;
        }

        // One request per competition, not per match. In practice that's one request full stop -
        // matches in play at the same moment nearly always belong to the same competition.
        foreach (var group in matches.GroupBy(m => m.CompetitionCode))
        {
            // The provider files fixtures under their UTC date, which isn't always today's UK date -
            // a 00:30 BST kick-off belongs to yesterday as far as it's concerned. Asking for the
            // range the matches themselves span sidesteps that instead of guessing at it.
            var utcKickoffs = group.Select(m => UkClock.ToUtc(m.MatchDateTime)).ToList();
            var fromUtcDate = DateOnly.FromDateTime(utcKickoffs.Min());
            var toUtcDate = DateOnly.FromDateTime(utcKickoffs.Max());

            IReadOnlyList<ExternalMatchScore> scores;
            try
            {
                scores = await _externalMatchDataService.GetScoresAsync(
                    group.Key, fromUtcDate, toUtcDate, cancellationToken);
            }
            catch (ExternalApiRateLimitedException ex)
            {
                // Expected, not exceptional: something else spent the budget. Stop this pass rather
                // than working through the remaining competitions - they'd all be refused too.
                _logger.LogInformation("Live score refresh skipped: {Reason}", ex.Message);
                summary.SkippedReason = ex.Message;
                return summary;
            }
            catch (HttpRequestException ex)
            {
                // A provider outage shouldn't take the poller down with it - log and try again next
                // pass. Warning rather than Error: a delayed free-tier feed dropping requests is a
                // known condition, and this lands in the admin Error Log either way.
                _logger.LogWarning(ex, "Could not fetch live scores for competition {CompetitionCode}", group.Key);
                continue;
            }

            summary.ScoresChanged += Apply(group.ToList(), scores, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return summary;
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetNextRefreshDelayAsync(CancellationToken cancellationToken = default)
    {
        var now = UkClock.Now;

        var matches = await GetMatchesInPlayAsync(now, cancellationToken);
        if (matches.Count > 0)
        {
            return TimeSpan.FromSeconds(_options.Value.LiveScorePollSeconds);
        }

        var nextKickoff = await PollableMatches()
            .Where(m => m.MatchDateTime > now)
            .OrderBy(m => m.MatchDateTime)
            .Select(m => (DateTime?)m.MatchDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextKickoff is null)
        {
            return MaxIdleDelay;
        }

        var untilKickoff = nextKickoff.Value - now;

        return untilKickoff > MaxIdleDelay ? MaxIdleDelay
            : untilKickoff < MinDelay ? MinDelay
            : untilKickoff;
    }

    /// <inheritdoc />
    public async Task<Result<MatchLiveScoreModel>> SaveAdminScoreAsync(
        Guid matchId,
        int homeTeamGoals,
        int awayTeamGoals,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (homeTeamGoals < 0 || awayTeamGoals < 0)
        {
            return Result.Fail<MatchLiveScoreModel>(new ConflictError("A live score can't be negative."));
        }

        var match = await _dbContext.Match.FirstOrDefaultAsync(m => m.MatchID == matchId, cancellationToken);
        if (match is null)
        {
            return Result.Fail<MatchLiveScoreModel>(new NotFoundError("The match could not be found."));
        }

        if (match.MatchPlayed)
        {
            return Result.Fail<MatchLiveScoreModel>(new ConflictError(
                "This match already has a confirmed result - edit that on the Process Results page instead."));
        }

        var now = UkClock.Now;
        var liveScore = await _dbContext.MatchLiveScore.FirstOrDefaultAsync(s => s.MatchID == matchId, cancellationToken);

        if (liveScore is null)
        {
            liveScore = new MatchLiveScore { MatchID = matchId };
            await _dbContext.AddAsync(liveScore, cancellationToken);
        }
        else if (liveScore.HomeTeamGoals == homeTeamGoals && liveScore.AwayTeamGoals == awayTeamGoals)
        {
            // Re-saving the same scoreline shouldn't make it look freshly confirmed. It doesn't
            // extend the admin hold either (see ShouldAccept) - UpdatedDateTime is what measures it,
            // and re-typing a score the site is already showing isn't taking control of anything.
            return Result.Ok(ToModel(liveScore));
        }

        liveScore.HomeTeamGoals = homeTeamGoals;
        liveScore.AwayTeamGoals = awayTeamGoals;
        liveScore.Source = LiveScoreSource.Admin;
        liveScore.UpdatedDateTime = now;
        liveScore.UpdatedByUserID = userId;

        // Status is left as the provider last reported it. Clearing it would un-retire a match the
        // provider has already called FINISHED, putting it back in the poll set to be overwritten
        // on the next pass - the opposite of what an admin correcting a final score wants.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(ToModel(liveScore));
    }

    /// <summary>
    /// Merges what the provider reported into the stored scores, returning how many scorelines
    /// actually changed.
    /// </summary>
    /// <param name="matches">The matches in play this pass covers.</param>
    /// <param name="scores">Everything the provider reported for the relevant competition and day.</param>
    /// <param name="now">The current time, stamped on anything written.</param>
    private int Apply(IReadOnlyList<MatchInPlay> matches, IReadOnlyList<ExternalMatchScore> scores, DateTime now)
    {
        var byExternalId = scores
            .GroupBy(s => s.ExternalMatchID)
            .ToDictionary(g => g.Key, g => g.First());

        var changed = 0;

        foreach (var match in matches)
        {
            if (!byExternalId.TryGetValue(match.ExternalMatchID, out var reported)
                || reported.HomeTeamGoals is null
                || reported.AwayTeamGoals is null)
            {
                continue;
            }

            var liveScore = match.LiveScore;
            if (liveScore is null)
            {
                liveScore = new MatchLiveScore
                {
                    MatchID = match.MatchID,
                    HomeTeamGoals = reported.HomeTeamGoals.Value,
                    AwayTeamGoals = reported.AwayTeamGoals.Value,
                    Status = reported.Status,
                    Source = LiveScoreSource.Api,
                    UpdatedDateTime = now,
                    LastPolledDateTime = now,
                };

                _dbContext.MatchLiveScore.Add(liveScore);
                changed++;
                continue;
            }

            // Whether or not the reported score is taken, we did just hear from the provider - and
            // that's what the other-worker check above reads.
            liveScore.LastPolledDateTime = now;
            liveScore.Status = reported.Status;

            if (!ShouldAccept(reported, liveScore, now))
            {
                _logger.LogInformation(
                    "Held match {MatchID}'s admin score {StoredHome}-{StoredAway} over the provider's {ReportedHome}-{ReportedAway}",
                    match.MatchID, liveScore.HomeTeamGoals, liveScore.AwayTeamGoals, reported.HomeTeamGoals, reported.AwayTeamGoals);
                continue;
            }

            if (liveScore.HomeTeamGoals == reported.HomeTeamGoals && liveScore.AwayTeamGoals == reported.AwayTeamGoals)
            {
                continue;
            }

            // A scoreline going backwards means a goal has been disallowed, or the feed has wobbled.
            // Worth a line either way: it's the only trace left of a score the site showed and then
            // withdrew, and the first thing worth looking at when someone queries a result.
            if (reported.HomeTeamGoals < liveScore.HomeTeamGoals || reported.AwayTeamGoals < liveScore.AwayTeamGoals)
            {
                _logger.LogInformation(
                    "Match {MatchID}'s score went down: {StoredHome}-{StoredAway} to {ReportedHome}-{ReportedAway} ({Status})",
                    match.MatchID, liveScore.HomeTeamGoals, liveScore.AwayTeamGoals, reported.HomeTeamGoals, reported.AwayTeamGoals, reported.Status);
            }

            liveScore.HomeTeamGoals = reported.HomeTeamGoals.Value;
            liveScore.AwayTeamGoals = reported.AwayTeamGoals.Value;
            liveScore.Source = LiveScoreSource.Api;
            liveScore.UpdatedDateTime = now;
            liveScore.UpdatedByUserID = null;
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// Whether a reported score should replace the one already held.
    ///
    /// The provider is believed in both directions. A score going down mid-match is a real thing
    /// rather than a stale feed - VAR disallows a goal that was given on the pitch - and refusing a
    /// decrease leaves a withdrawn goal standing for the rest of the match, which is far worse than
    /// the one-poll flicker that refusing it avoids.
    ///
    /// The one thing that outranks the feed is an admin who has just entered a score, on the
    /// assumption they are watching the match and the feed is behind. That holds for
    /// <see cref="AdminScoreHold"/> only, after which the feed is assumed to have caught up and
    /// takes over again. A settled full-time score is taken whatever the source: an admin correcting
    /// that is doing so on a match polling has already retired.
    /// </summary>
    /// <param name="reported">The score the provider reported.</param>
    /// <param name="stored">The score currently held.</param>
    /// <param name="now">The current time, to measure the admin hold against.</param>
    private static bool ShouldAccept(ExternalMatchScore reported, MatchLiveScore stored, DateTime now)
    {
        if (reported.IsFinished)
        {
            return true;
        }

        if (!string.Equals(stored.Source, LiveScoreSource.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return now - stored.UpdatedDateTime >= AdminScoreHold;
    }

    /// <summary>
    /// The matches worth asking the provider about right now: kicked off, inside the poll window,
    /// no confirmed result, not already called FINISHED, and joined to a competition and fixture the
    /// provider actually knows about.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<IReadOnlyList<MatchInPlay>> GetMatchesInPlayAsync(DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(-PollWindowHours);

        var matches = await PollableMatches()
            .Where(m => m.MatchDateTime <= now && m.MatchDateTime >= windowStart)
            .Select(m => new MatchInPlay
            {
                MatchID = m.MatchID,
                MatchDateTime = m.MatchDateTime,
                ExternalMatchID = m.ExternalMatchID!.Value,
                CompetitionCode = m.Competition.ExternalApiCompetitionCode!,
                LiveScore = m.MatchLiveScore,
            })
            .ToListAsync(cancellationToken);

        // Filtered here rather than in the query so it reads as the rule it is: a match the provider
        // has called FINISHED is done, and polling it again would only re-apply that final score over
        // any correction an admin has since made.
        return matches
            .Where(m => !string.Equals(m.LiveScore?.Status, ExternalMatchScore.FinishedStatus, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Matches the live-score integration can say anything about at all, whenever they kick off -
    /// the shared base for "in play now" and "next to kick off".
    /// </summary>
    private IQueryable<Match> PollableMatches()
    {
        return _dbContext.Match
            .Where(m => !m.MatchPlayed
                && m.ExternalMatchID != null
                && m.Competition.ExternalApiCompetitionCode != null);
    }

    private static MatchLiveScoreModel ToModel(MatchLiveScore liveScore)
    {
        return new MatchLiveScoreModel
        {
            MatchID = liveScore.MatchID,
            HomeTeamGoals = liveScore.HomeTeamGoals,
            AwayTeamGoals = liveScore.AwayTeamGoals,
            Status = liveScore.Status,
            Source = liveScore.Source,
            UpdatedDateTime = liveScore.UpdatedDateTime,
        };
    }

    /// <summary>A match in play, flattened with just what a refresh pass needs.</summary>
    private sealed class MatchInPlay
    {
        public Guid MatchID { get; init; }

        /// <summary>Kick-off, in the naive UK wall-clock terms MatchDateTime is stored in.</summary>
        public DateTime MatchDateTime { get; init; }

        public int ExternalMatchID { get; init; }

        public string CompetitionCode { get; init; } = "";

        /// <summary>The stored score, or null if we've never had one for this match.</summary>
        public MatchLiveScore? LiveScore { get; init; }
    }
}
