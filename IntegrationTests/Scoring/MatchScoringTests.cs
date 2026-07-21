using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using System.Data;

namespace Predictathon.IntegrationTests.Scoring;

/// <summary>
/// Exercises the dbo.MatchPredictionScoreSet stored procedure directly - this is where the 3/2/1/0
/// scoring rule actually lives (see LeagueTableGet.sql and MatchPredictionScoreSet.sql), not in any
/// C# code, so it can only meaningfully be covered against a real SQL Server instance.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchScoringTests
{
    private readonly DatabaseFixture _fixture;

    public MatchScoringTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MatchPredictionScoreSet_ScoresPredictionsAccordingToAccuracy()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext, allowTwoPointers: false);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);
        var match = await CreateMatchAsync(dbContext, competition.CompetitionID, homeTeamId, awayTeamId,
            matchPlayed: true, homeTeamGoals: 3, awayTeamGoals: 1);

        var perfectUser = await CreateUserAsync(dbContext);
        var correctOutcomeUser = await CreateUserAsync(dbContext);
        var wrongOutcomeUser = await CreateUserAsync(dbContext);
        var drawUser = await CreateUserAsync(dbContext);

        await CreatePredictionAsync(dbContext, match.MatchID, perfectUser.Id, homeGoals: 3, awayGoals: 1);
        await CreatePredictionAsync(dbContext, match.MatchID, correctOutcomeUser.Id, homeGoals: 2, awayGoals: 0);
        await CreatePredictionAsync(dbContext, match.MatchID, wrongOutcomeUser.Id, homeGoals: 0, awayGoals: 1);
        await CreatePredictionAsync(dbContext, match.MatchID, drawUser.Id, homeGoals: 1, awayGoals: 1);
        await dbContext.SaveChangesAsync();

        try
        {
            await RunMatchPredictionScoreSetAsync(dbContext, match.MatchID);

            // AsNoTracking - without it, EF's identity map would hand back the already-tracked
            // Prediction instances from CreatePredictionAsync unchanged, masking the update the raw
            // SQL stored proc just made to the underlying row.
            var scores = await dbContext.Prediction
                .AsNoTracking()
                .Where(p => p.MatchID == match.MatchID)
                .ToDictionaryAsync(p => p.UserID, p => p.Score);

            scores[perfectUser.Id].Should().Be(3, "an exact scoreline match is a 3-pointer");
            scores[correctOutcomeUser.Id].Should().Be(1, "the right outcome with the wrong scoreline is a 1-pointer when AllowTwoPointers is off");
            scores[wrongOutcomeUser.Id].Should().Be(0, "predicting the wrong outcome scores nothing");
            scores[drawUser.Id].Should().Be(0, "predicting a draw when the match wasn't a draw scores nothing");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID],
                [perfectUser.Id, correctOutcomeUser.Id, wrongOutcomeUser.Id, drawUser.Id], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task MatchPredictionScoreSet_AwardsTwoPointer_WhenCompetitionAllowsAndScorelineMatchesExactly()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext, allowTwoPointers: true);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);
        var match = await CreateMatchAsync(dbContext, competition.CompetitionID, homeTeamId, awayTeamId,
            matchPlayed: true, homeTeamGoals: 3, awayTeamGoals: 1);

        var twoPointerUser = await CreateUserAsync(dbContext);

        // Correct outcome AND the winning side's goals match exactly - a 2-pointer when the
        // competition allows it (only the losing/away side's goals differ from the actual result).
        await CreatePredictionAsync(dbContext, match.MatchID, twoPointerUser.Id, homeGoals: 3, awayGoals: 0);
        await dbContext.SaveChangesAsync();

        try
        {
            await RunMatchPredictionScoreSetAsync(dbContext, match.MatchID);

            var prediction = await dbContext.Prediction.AsNoTracking().SingleAsync(p => p.MatchID == match.MatchID && p.UserID == twoPointerUser.Id);

            prediction.Score.Should().Be(2);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [twoPointerUser.Id], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task MatchPredictionScoreSet_LeavesScoreNull_ForUnplayedMatch()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext, allowTwoPointers: false);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);
        var match = await CreateMatchAsync(dbContext, competition.CompetitionID, homeTeamId, awayTeamId,
            matchPlayed: false, homeTeamGoals: null, awayTeamGoals: null);

        var user = await CreateUserAsync(dbContext);
        await CreatePredictionAsync(dbContext, match.MatchID, user.Id, homeGoals: 1, awayGoals: 0);
        await dbContext.SaveChangesAsync();

        try
        {
            await RunMatchPredictionScoreSetAsync(dbContext, match.MatchID);

            var prediction = await dbContext.Prediction.AsNoTracking().SingleAsync(p => p.MatchID == match.MatchID && p.UserID == user.Id);

            prediction.Score.Should().BeNull();
            prediction.GoalDifference.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [user.Id], [homeTeamId, awayTeamId]);
        }
    }

    private static async Task RunMatchPredictionScoreSetAsync(Infrastructure.Persistence.ApplicationDbContext dbContext, Guid matchId)
    {
        var parameters = new List<SqlParameter> { new("@MatchID", SqlDbType.UniqueIdentifier) { Value = matchId } };
        await ((Predictathon.Application.Interfaces.Persistence.IGenericDbContext)dbContext)
            .CallStoredProcedureAsync("MatchPredictionScoreSet", parameters);
    }

    private static async Task<Competition> CreateCompetitionAsync(Infrastructure.Persistence.ApplicationDbContext dbContext, bool allowTwoPointers)
    {
        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            AllowTwoPointers = allowTwoPointers,
        };
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        return competition;
    }

    private static async Task<(Guid HomeTeamId, Guid AwayTeamId)> CreateTeamsAsync(Infrastructure.Persistence.ApplicationDbContext dbContext)
    {
        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);
        await dbContext.SaveChangesAsync();
        return (home.TeamID, away.TeamID);
    }

    private static async Task<Match> CreateMatchAsync(
        Infrastructure.Persistence.ApplicationDbContext dbContext,
        Guid competitionId,
        Guid homeTeamId,
        Guid awayTeamId,
        bool matchPlayed,
        int? homeTeamGoals,
        int? awayTeamGoals)
    {
        var match = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = DateTime.UtcNow.AddDays(-1),
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchPlayed = matchPlayed,
            HomeTeamGoals = homeTeamGoals,
            AwayTeamGoals = awayTeamGoals,
        };
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();
        return match;
    }

    private static async Task<ApplicationUser> CreateUserAsync(Infrastructure.Persistence.ApplicationDbContext dbContext)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"integration-{Guid.NewGuid():N}",
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task CreatePredictionAsync(
        Infrastructure.Persistence.ApplicationDbContext dbContext,
        Guid matchId,
        Guid userId,
        int homeGoals,
        int awayGoals)
    {
        dbContext.Prediction.Add(new Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = matchId,
            UserID = userId,
            HomeTeamGoals = homeGoals,
            AwayTeamGoals = awayGoals,
        });
    }

    private static async Task CleanUpAsync(
        Infrastructure.Persistence.ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
