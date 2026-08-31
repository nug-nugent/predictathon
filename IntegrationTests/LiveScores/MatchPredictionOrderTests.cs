using FluentAssertions;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.LiveScores;

/// <summary>
/// Exercises the ordering of dbo.MatchPredictionListGet, the Live page's All Predictions list. The
/// list has always come back best-first on confirmed points, but a match in play has no confirmed
/// points at all, so the ordering has to fall to the projection instead - and that can only be
/// checked against a real database, since the projection and the ordering both live in the procedure.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchPredictionOrderTests
{
    private readonly DatabaseFixture _fixture;

    public MatchPredictionOrderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_PutsTheHighestProjectedScoreFirst_WhileTheMatchIsInPlay()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var match = NewMatch(competition, now.AddMinutes(-30));

        // Usernames run the opposite way to the projected scores, so a list still ordered by name
        // would come back in exactly the reverse of what is expected here.
        var perfect = NewUser("aaa");     // 2-1 against a live 2-1: three points.
        var winnerGoals = NewUser("bbb"); // Home win with the winner's goals right: two.
        var result = NewUser("ccc");      // Home win, both goals wrong: one.
        var wrong = NewUser("ddd");       // Away win: nothing.
        var absentee = NewUser("eee");    // No prediction at all: nothing to project.

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(perfect, winnerGoals, result, wrong, absentee);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(
            NewRegistration(competition, perfect),
            NewRegistration(competition, winnerGoals),
            NewRegistration(competition, result),
            NewRegistration(competition, wrong),
            NewRegistration(competition, absentee));
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 2, 1, now));

        // The bottom two carry confirmed points from a result that has since been reopened - what a
        // fixture correction leaves behind, and what the sample data does deliberately. Points that
        // no longer apply must not outrank the projection the reader is actually being shown.
        var stalePerfect = NewPrediction(match, wrong, 0, 1);
        stalePerfect.Score = 3;
        stalePerfect.GoalDifference = 0;

        dbContext.Prediction.AddRange(
            NewPrediction(match, perfect, 2, 1),
            NewPrediction(match, winnerGoals, 2, 0),
            NewPrediction(match, result, 3, 0),
            stalePerfect);

        await dbContext.SaveChangesAsync();

        try
        {
            var predictions = await GetPredictionsAsync(dbContext, match.MatchID);

            predictions.Select(p => p.ProjectedScore).Should().Equal(3, 2, 1, 0, null);
            predictions.Select(p => p.UserID).Should().Equal(
                [perfect.Id, winnerGoals.Id, result.Id, wrong.Id, absentee.Id],
                "the list reads best-first while the match is live, with anyone who did not predict at the bottom");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [perfect, winnerGoals, result, wrong, absentee]);
        }
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_SeparatesEqualProjectedScores_OnGoalDifference()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var match = NewMatch(competition, now.AddMinutes(-30));

        // Both call the home win without the goals, so both project one point; 3-0 is two goals away
        // from the live 2-1 and 4-0 is three, and the name ordering would put them the other way up.
        var closer = NewUser("zzz");
        var further = NewUser("aaa");

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(closer, further);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(NewRegistration(competition, closer), NewRegistration(competition, further));
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 2, 1, now));
        dbContext.Prediction.AddRange(NewPrediction(match, closer, 3, 0), NewPrediction(match, further, 4, 0));

        await dbContext.SaveChangesAsync();

        try
        {
            var predictions = await GetPredictionsAsync(dbContext, match.MatchID);

            predictions.Select(p => p.UserID).Should().Equal([closer.Id, further.Id],
                "level on projected points, the closer prediction goes first - the same tie-break confirmed points use");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [closer, further]);
        }
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_StillOrdersOnConfirmedPoints_OnceTheResultIsIn()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var match = NewMatch(competition, now.AddHours(-3));
        match.MatchPlayed = true;
        match.HomeTeamGoals = 2;
        match.AwayTeamGoals = 1;

        var scorer = NewUser("zzz");
        var nonScorer = NewUser("aaa");

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(scorer, nonScorer);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(NewRegistration(competition, scorer), NewRegistration(competition, nonScorer));

        // The last live score before full time disagrees with the confirmed result, so a list that
        // let the projection near a played match would order these two the wrong way round.
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 0, 1, now.AddHours(-2)));

        var scored = NewPrediction(match, scorer, 2, 1);
        scored.Score = 3;
        scored.GoalDifference = 0;
        var unscored = NewPrediction(match, nonScorer, 0, 1);
        unscored.Score = 0;
        unscored.GoalDifference = -2;
        dbContext.Prediction.AddRange(scored, unscored);

        await dbContext.SaveChangesAsync();

        try
        {
            var predictions = await GetPredictionsAsync(dbContext, match.MatchID);

            predictions.Select(p => p.UserID).Should().Equal([scorer.Id, nonScorer.Id]);
            predictions.Should().OnlyContain(p => p.ProjectedScore == null,
                "a confirmed result has nothing to project - Score is the real answer");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [scorer, nonScorer]);
        }
    }

    /// <summary>
    /// Reads the match's predictions through the service, failing the test if the procedure refused
    /// the call.
    /// </summary>
    /// <param name="dbContext">The database context to read through.</param>
    /// <param name="matchId">The match whose predictions are wanted.</param>
    private static async Task<IReadOnlyList<MatchPredictionListItem>> GetPredictionsAsync(
        ApplicationDbContext dbContext,
        Guid matchId)
    {
        var result = await new PredictionService(dbContext).GetMatchPredictionsAsync(matchId);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static Competition NewCompetition(DateTime now, bool allowTwoPointers) => new()
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = $"Integration Test {Guid.NewGuid():N}",
        StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
        EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        AllowTwoPointers = allowTwoPointers,
    };

    private static Match NewMatch(Competition competition, DateTime kickoff) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = competition.CompetitionID,
        MatchDateTime = kickoff,
        MatchPlayed = false,
    };

    private static ApplicationUser NewUser(string prefix) => new()
    {
        Id = Guid.NewGuid(),
        UserName = $"{prefix}-{Guid.NewGuid():N}",
    };

    private static UserCompetition NewRegistration(Competition competition, ApplicationUser user) => new()
    {
        UserCompetitionID = Guid.NewGuid(),
        UserID = user.Id,
        CompetitionID = competition.CompetitionID,
    };

    private static Prediction NewPrediction(Match match, ApplicationUser user, int home, int away) => new()
    {
        PredictionID = Guid.NewGuid(),
        MatchID = match.MatchID,
        UserID = user.Id,
        HomeTeamGoals = home,
        AwayTeamGoals = away,
    };

    private static MatchLiveScore NewLiveScore(Guid matchId, int home, int away, DateTime now) => new()
    {
        MatchID = matchId,
        HomeTeamGoals = home,
        AwayTeamGoals = away,
        Status = "IN_PLAY",
        Source = LiveScoreSource.Api,
        UpdatedDateTime = now,
        LastPolledDateTime = now,
    };

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Competition competition,
        IReadOnlyList<Match> matches,
        IReadOnlyList<ApplicationUser> users)
    {
        var matchIds = matches.Select(m => m.MatchID).ToList();
        var userIds = users.Select(u => u.Id).ToList();

        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.MatchLiveScore.RemoveRange(dbContext.MatchLiveScore.Where(s => matchIds.Contains(s.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => userIds.Contains(uc.UserID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competition.CompetitionID));
        await dbContext.SaveChangesAsync();
    }
}
