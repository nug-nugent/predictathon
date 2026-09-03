using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.IntegrationTests.TestDoubles;

namespace Predictathon.IntegrationTests.LeagueTable;

/// <summary>
/// The league tables are cached, which is only safe while the things that change them throw the
/// cached copies away. This covers that end to end - a real result processed through MatchService,
/// and the table read back through LeagueTableService off the same cache - because the failure it
/// guards against is invisible from either side alone: everything still returns a perfectly valid
/// league table, just the one from before the result went in.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LeagueDataCacheInvalidationTests
{
    private readonly DatabaseFixture _fixture;

    public LeagueDataCacheInvalidationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProcessingAResult_DropsTheCachedLeagueTable()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.Add(competition);

        var predictor = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"predictor-{Guid.NewGuid():N}" };
        dbContext.Users.Add(predictor);
        dbContext.UserCompetition.Add(new UserCompetition
        {
            UserCompetitionID = Guid.NewGuid(),
            UserID = predictor.Id,
            CompetitionID = competition.CompetitionID,
        });

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        // Kicked off, no result yet - so the table starts with this user on nothing.
        var match = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddHours(-3),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = false,
        };
        dbContext.Match.Add(match);

        dbContext.Prediction.Add(new Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = predictor.Id,
            HomeTeamGoals = 2,
            AwayTeamGoals = 1,
        });

        await dbContext.SaveChangesAsync();

        try
        {
            // One cache between the two services, which is what the running app has - a singleton
            // shared by every scoped service that touches it.
            var cache = new LeagueDataCache();
            var leagueTableService = new LeagueTableService(dbContext, new StubAvatarService(), cache);
            var matchService = new MatchService(
                new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper()),
                dbContext,
                dbContext,
                cache);

            var before = await leagueTableService.GetLeagueTableAsync(competition.CompetitionID);
            before.Single(r => r.UserID == predictor.Id).Score.Should().Be(0);

            var result = await matchService.SaveResultAsync(match.MatchID, homeTeamGoals: 2, awayTeamGoals: 1);
            result.IsSuccess.Should().BeTrue();

            var after = await leagueTableService.GetLeagueTableAsync(competition.CompetitionID);

            after.Single(r => r.UserID == predictor.Id).Score.Should().Be(
                3,
                "processing a result has to invalidate the cached league table - otherwise the table keeps reporting the standings from before the result went in");
        }
        finally
        {
            dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => p.MatchID == match.MatchID));
            dbContext.Match.RemoveRange(dbContext.Match.Where(m => m.MatchID == match.MatchID));
            dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => uc.CompetitionID == competition.CompetitionID));
            dbContext.Users.RemoveRange(dbContext.Users.Where(u => u.Id == predictor.Id));
            dbContext.Team.RemoveRange(dbContext.Team.Where(t => t.TeamID == home.TeamID || t.TeamID == away.TeamID));
            dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competition.CompetitionID));
            await dbContext.SaveChangesAsync();
        }
    }
}
