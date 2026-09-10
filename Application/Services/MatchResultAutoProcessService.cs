using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Application.Options;

namespace Predictathon.Application.Services;

[ScopedService]
public class MatchResultAutoProcessService : IMatchResultAutoProcessService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMatchService _matchService;
    private readonly IOptions<FootballDataApiOptions> _options;
    private readonly ILogger<MatchResultAutoProcessService> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="MatchResultAutoProcessService"/> class.
    /// </summary>
    /// <param name="dbContext">Source of the finished-but-unprocessed matches to confirm.</param>
    /// <param name="matchService">Confirms each result, the same way the Process Results page does.</param>
    /// <param name="options">Supplies the switch that turns auto-processing off.</param>
    /// <param name="logger">Logs what each match's confirmation did.</param>
    public MatchResultAutoProcessService(
        IApplicationDbContext dbContext,
        IMatchService matchService,
        IOptions<FootballDataApiOptions> options,
        ILogger<MatchResultAutoProcessService> logger)
    {
        _dbContext = dbContext;
        _matchService = matchService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultAutoProcessSummary> ProcessFinishedMatchesAsync(CancellationToken cancellationToken = default)
    {
        var summary = new ResultAutoProcessSummary();

        if (!_options.Value.AutoProcessFinishedResults)
        {
            summary.SkippedReason = "auto-processing is switched off";
            return summary;
        }

        var finished = await GetFinishedUnprocessedMatchesAsync(UkClock.Now, cancellationToken);

        foreach (var match in finished)
        {
            // Routed through the same call the Process Results page makes, rather than writing the
            // columns here: that's the one place a result gets confirmed, and it's what rescores the
            // match's predictions and drops the cached league tables. An automatic result that
            // skipped either would leave the site showing a played match nobody had scored.
            // No user id: a null ProcessedByUserID is what records that nobody confirmed this by
            // hand, which is the whole distinction dbo.Match's audit columns exist to draw.
            var result = await _matchService.SaveResultAsync(
                match.MatchID, match.HomeTeamGoals, match.AwayTeamGoals, processedByUserId: null, cancellationToken);

            if (result.IsFailed)
            {
                // Left for an admin rather than retried into a loop - the match keeps its finished
                // live score, so the next sweep will try again, and the Process Results page still
                // offers it in the meantime.
                _logger.LogWarning(
                    "Could not auto-process the result for match {MatchID} ({Home}-{Away}): {Reason}",
                    match.MatchID, match.HomeTeamGoals, match.AwayTeamGoals,
                    string.Join("; ", result.Errors.Select(e => e.Message)));

                summary.ResultsFailed++;
                continue;
            }

            _logger.LogInformation(
                "Auto-processed the result for match {MatchID} from the {Source} score {Home}-{Away}.",
                match.MatchID, match.Source, match.HomeTeamGoals, match.AwayTeamGoals);

            summary.ResultsProcessed++;
        }

        return summary;
    }

    /// <summary>
    /// The matches whose result can be confirmed without an admin: no confirmed result yet, a stored
    /// live score the provider has called finished, and far enough past kick-off to be eligible.
    ///
    /// Read from the stored live score rather than from a fresh provider response, which is what
    /// makes this cheap enough to run on every poll pass and means an admin's own correction on top
    /// of the provider's final is the score that gets confirmed - they saw something the feed
    /// didn't. The eligibility filter is applied here as well as inside the save so an ineligible
    /// match is simply skipped rather than counted as a failure; in practice a finished match is
    /// always past it, and only an odd feed (an abandoned match called finished early) isn't.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<IReadOnlyList<FinishedMatch>> GetFinishedUnprocessedMatchesAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var eligibleFrom = MatchResultWindow.EligibleFrom(now);

        var candidates = await _dbContext.Match
            .Where(m => !m.MatchPlayed
                && m.MatchDateTime <= eligibleFrom
                && m.MatchLiveScore != null)
            .Select(m => new FinishedMatch
            {
                MatchID = m.MatchID,
                Status = m.MatchLiveScore!.Status,
                HomeTeamGoals = m.MatchLiveScore.HomeTeamGoals,
                AwayTeamGoals = m.MatchLiveScore.AwayTeamGoals,
                Source = m.MatchLiveScore.Source,
            })
            .ToListAsync(cancellationToken);

        // Compared in memory so the match is case-insensitive without depending on the database's
        // collation, matching how LiveScoreService reads the same column.
        return candidates
            .Where(m => string.Equals(m.Status, ExternalMatchScore.FinishedStatus, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>A match the provider has called finished, flattened with just what confirming it needs.</summary>
    private sealed class FinishedMatch
    {
        public Guid MatchID { get; init; }

        /// <summary>The provider's last reported status, which is what marks the match as finished.</summary>
        public string? Status { get; init; }

        public int HomeTeamGoals { get; init; }

        public int AwayTeamGoals { get; init; }

        /// <summary>Whether the score being confirmed came from the provider or an admin - see <see cref="Constants.LiveScoreSource"/>.</summary>
        public string Source { get; init; } = "";
    }
}
