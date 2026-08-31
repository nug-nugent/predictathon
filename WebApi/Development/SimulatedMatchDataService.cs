using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Common;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.WebApi.Development;

/// <summary>
/// Stands in for the real football-data.org client in the Docker dev stack, inventing plausible
/// in-play scores for the sample fixtures already in the database.
///
/// It exists because the dev stack can't use the real thing: there's no API key, and "Sample Cup"
/// is hand-authored data whose fixtures don't exist at football-data.org, so the poller would find
/// nothing to ask about and the whole live-score feature would be invisible locally. Swapped in by
/// configuration (FootballDataApi:UseSimulatedProvider) and never in Production - see Program.cs.
///
/// The scores it reports are a function of the match id and how long ago the match kicked off, so
/// they're deterministic (the same match always plays out the same way), monotonic (goals only ever
/// arrive, which is what the real feed does and what LiveScoreService's merge rule assumes) and
/// they move on their own while you watch - a match runs its ninety minutes, pauses at half time,
/// and finishes.
/// </summary>
public class SimulatedMatchDataService : IExternalMatchDataService
{
    private const int FirstHalfMinutes = 45;
    private const int HalfTimeMinutes = 15;
    private const int FullTimeMinutes = 90;

    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initialises a new instance of the <see cref="SimulatedMatchDataService"/> class.
    /// </summary>
    /// <param name="dbContext">Source of the fixtures to invent scores for.</param>
    public SimulatedMatchDataService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Always empty. Fixtures in the dev stack come from the sample seed scripts, not from an
    /// import, so inventing a fixture list here would only produce phantom reschedules on the
    /// Fixture Changes page. The import screens report "no fixtures returned", which is the honest
    /// answer for a competition the provider has never heard of.
    /// </summary>
    /// <param name="competitionCode">Ignored.</param>
    /// <param name="season">Ignored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExternalFixture>>([]);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalMatchScore>> GetScoresAsync(
        string competitionCode,
        DateOnly fromUtcDate,
        DateOnly toUtcDate,
        CancellationToken cancellationToken = default)
    {
        // The real provider is asked for a competition and a span of UTC days, and answers with
        // every fixture it holds in that window - so this does the same, from the fixtures we have.
        var fromUtc = fromUtcDate.ToDateTime(TimeOnly.MinValue);
        var toUtc = toUtcDate.ToDateTime(TimeOnly.MaxValue);

        var matches = await _dbContext.Match
            .Where(m => m.ExternalMatchID != null && m.Competition.ExternalApiCompetitionCode == competitionCode)
            .Select(m => new { m.MatchID, m.MatchDateTime, ExternalMatchID = m.ExternalMatchID!.Value })
            .ToListAsync(cancellationToken);

        var now = UkClock.Now;

        return matches
            .Where(m =>
            {
                var kickoffUtc = UkClock.ToUtc(m.MatchDateTime);
                return kickoffUtc >= fromUtc && kickoffUtc <= toUtc;
            })
            .Select(m => Simulate(m.MatchID, m.ExternalMatchID, m.MatchDateTime, now))
            .ToList();
    }

    /// <summary>
    /// Plays a match out from its kick-off time: where it is on the clock, and how many of its goals
    /// have gone in by now.
    /// </summary>
    /// <param name="matchId">Seeds this match's goals, so it always plays out the same way.</param>
    /// <param name="externalMatchId">The id the caller will match this back up by.</param>
    /// <param name="kickoff">Kick-off, in the UK wall-clock terms MatchDateTime is stored in.</param>
    /// <param name="now">The current time.</param>
    private static ExternalMatchScore Simulate(Guid matchId, int externalMatchId, DateTime kickoff, DateTime now)
    {
        var elapsed = (int)(now - kickoff).TotalMinutes;

        if (elapsed < 0)
        {
            // Not started: the provider reports the fixture with no score at all, which is also what
            // exercises LiveScoreService's "ignore a report with no goals in it" path.
            return new ExternalMatchScore { ExternalMatchID = externalMatchId, Status = "TIMED" };
        }

        var status = elapsed switch
        {
            < FirstHalfMinutes => "IN_PLAY",
            < FirstHalfMinutes + HalfTimeMinutes => "PAUSED",
            < FullTimeMinutes + HalfTimeMinutes => "IN_PLAY",
            _ => ExternalMatchScore.FinishedStatus,
        };

        var goals = GoalScript(matchId).Where(g => g.Minute <= MatchMinute(elapsed)).ToList();

        return new ExternalMatchScore
        {
            ExternalMatchID = externalMatchId,
            Status = status,
            HomeTeamGoals = goals.Count(g => g.IsHome),
            AwayTeamGoals = goals.Count(g => !g.IsHome),
        };
    }

    /// <summary>
    /// Converts minutes since kick-off into minutes on the match clock, which stops for half time -
    /// without this a goal scripted for the 50th minute would appear while the players are still in
    /// the dressing room.
    /// </summary>
    /// <param name="elapsed">Real minutes since kick-off.</param>
    private static int MatchMinute(int elapsed)
    {
        if (elapsed <= FirstHalfMinutes)
        {
            return elapsed;
        }

        return Math.Min(elapsed - HalfTimeMinutes, FullTimeMinutes);
    }

    /// <summary>
    /// The goals this match will contain and the minutes they arrive in, derived from the match id
    /// so every worker process and every call agrees on them without storing anything.
    /// </summary>
    /// <param name="matchId">The match to script.</param>
    private static IReadOnlyList<(int Minute, bool IsHome)> GoalScript(Guid matchId)
    {
        // Seeded from the id's own bytes rather than GetHashCode, whose value isn't guaranteed to be
        // stable between processes - two API workers would otherwise script the same match
        // differently and the score would flip about.
        var random = new Random(BitConverter.ToInt32(matchId.ToByteArray(), 0));

        return Enumerable.Range(0, random.Next(0, 6))
            .Select(_ => (Minute: random.Next(1, FullTimeMinutes + 1), IsHome: random.Next(2) == 0))
            .OrderBy(goal => goal.Minute)
            .ToList();
    }
}
