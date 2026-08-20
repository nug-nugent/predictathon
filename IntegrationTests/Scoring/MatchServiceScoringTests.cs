using FluentAssertions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.Scoring;

/// <summary>
/// Verifies that MatchService.SaveResultAsync and MatchService.Update both trigger
/// dbo.MatchPredictionScoreSet (see MatchScoringTests for the SP's own scoring-rule coverage) -
/// the legacy WebForms app called this after every match save (MatchManager.Save), and that call
/// was dropped in the rebuild, silently leaving every prediction's Score/GoalDifference unset.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchServiceScoringTests
{
    private readonly DatabaseFixture _fixture;

    public MatchServiceScoringTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext);
    }

    [Fact]
    public async Task SaveResultAsync_RecalculatesPredictionScores()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var match = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddHours(-3),
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchPlayed = false,
        };
        dbContext.Match.Add(match);

        var perfectUser = await CreateUserAsync(dbContext);
        var wrongUser = await CreateUserAsync(dbContext);
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = perfectUser.Id, HomeTeamGoals = 2, AwayTeamGoals = 1 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = wrongUser.Id, HomeTeamGoals = 0, AwayTeamGoals = 2 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeService(dbContext);

            var result = await service.SaveResultAsync(match.MatchID, homeTeamGoals: 2, awayTeamGoals: 1);

            result.IsSuccess.Should().BeTrue();

            var scores = await dbContext.Prediction
                .AsNoTracking()
                .Where(p => p.MatchID == match.MatchID)
                .ToDictionaryAsync(p => p.UserID, p => p.Score);

            scores[perfectUser.Id].Should().Be(3, "SaveResultAsync should trigger MatchPredictionScoreSet, not just save the match's own goals");
            scores[wrongUser.Id].Should().Be(0);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [perfectUser.Id, wrongUser.Id], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task Update_RecalculatesPredictionScores_WhenCorrectingAPlayedMatchsResult()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        // Recorded 1-1, so a 2-1 prediction is a wrong-outcome 0-pointer - about to be corrected.
        var match = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddHours(-3),
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchPlayed = true,
            HomeTeamGoals = 1,
            AwayTeamGoals = 1,
        };
        dbContext.Match.Add(match);

        var user = await CreateUserAsync(dbContext);
        dbContext.Prediction.Add(new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = user.Id, HomeTeamGoals = 2, AwayTeamGoals = 1, Score = 0, GoalDifference = -1 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeService(dbContext);

            var correctedModel = new MatchModel
            {
                MatchID = match.MatchID,
                CompetitionID = competition.CompetitionID,
                MatchDateTime = match.MatchDateTime,
                HomeTeamID = homeTeamId,
                AwayTeamID = awayTeamId,
                HomeTeamGoals = 2,
                AwayTeamGoals = 1,
                MatchPlayed = true,
            };

            var result = await service.Update(match.MatchID, correctedModel);

            result.IsSuccess.Should().BeTrue();

            var prediction = await dbContext.Prediction.AsNoTracking().SingleAsync(p => p.MatchID == match.MatchID && p.UserID == user.Id);

            prediction.Score.Should().Be(3, "correcting the result via Update should trigger MatchPredictionScoreSet too, matching legacy MatchManager.Save behaviour");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [user.Id], [homeTeamId, awayTeamId]);
        }
    }

    private static async Task<Competition> CreateCompetitionAsync(ApplicationDbContext dbContext)
    {
        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        return competition;
    }

    private static async Task<(Guid HomeTeamId, Guid AwayTeamId)> CreateTeamsAsync(ApplicationDbContext dbContext)
    {
        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);
        await dbContext.SaveChangesAsync();
        return (home.TeamID, away.TeamID);
    }

    private static async Task<ApplicationUser> CreateUserAsync(ApplicationDbContext dbContext)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"integration-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
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
