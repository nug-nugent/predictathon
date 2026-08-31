using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Common;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class MatchServiceTests
{
    private static (InMemoryApplicationDbContext DbContext, MatchService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        var service = new MatchService(dependencyAggregate, dbContext, dbContext);
        return (dbContext, service);
    }

    [Fact]
    public async Task GetForAdminAsync_OrdersByMatchDateTimeThenHomeTeamName()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        var arsenal = new DomainEntities.Team { TeamID = Guid.NewGuid(), TeamName = "Arsenal", ShortName = "ARS" };
        var chelsea = new DomainEntities.Team { TeamID = Guid.NewGuid(), TeamName = "Chelsea", ShortName = "CHE" };
        dbContext.Team.AddRange(arsenal, chelsea);

        var kickoff = new DateTime(2026, 8, 22, 15, 0, 0);
        dbContext.Match.AddRange(
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = chelsea.TeamID },
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = arsenal.TeamID },
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff.AddDays(-1), HomeTeamID = chelsea.TeamID });
        await dbContext.SaveChangesAsync();

        var matches = await service.GetForAdminAsync(competitionId, includePlayed: true);

        matches.Select(m => m.HomeTeamID).Should().Equal(chelsea.TeamID, arsenal.TeamID, chelsea.TeamID);
    }

    [Fact]
    public async Task GetForProcessingAsync_OrdersByMatchDateTimeThenHomeTeamName()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        var arsenal = new DomainEntities.Team { TeamID = Guid.NewGuid(), TeamName = "Arsenal", ShortName = "ARS" };
        var chelsea = new DomainEntities.Team { TeamID = Guid.NewGuid(), TeamName = "Chelsea", ShortName = "CHE" };
        dbContext.Team.AddRange(arsenal, chelsea);

        var kickoff = UkClock.Now.AddHours(-3);
        dbContext.Match.AddRange(
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = chelsea.TeamID },
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = arsenal.TeamID },
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff.AddHours(-1), HomeTeamID = chelsea.TeamID });
        await dbContext.SaveChangesAsync();

        var matches = await service.GetForProcessingAsync(competitionId);

        matches.Select(m => m.HomeTeamID).Should().Equal(chelsea.TeamID, arsenal.TeamID, chelsea.TeamID);
    }

    [Fact]
    public async Task GetForProcessingAsync_ExcludesMatchesStillInsideTheNinetyMinuteResultWindow()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        var team = new DomainEntities.Team { TeamID = Guid.NewGuid(), TeamName = "Arsenal", ShortName = "ARS" };
        dbContext.Team.Add(team);

        // SaveResultAsync refuses a result until 90 minutes after kick-off, so a match that started
        // half an hour ago has no business being offered on the Process Results page.
        var inPlay = new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = UkClock.Now.AddMinutes(-30), HomeTeamID = team.TeamID };
        var finished = new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = UkClock.Now.AddMinutes(-120), HomeTeamID = team.TeamID };
        dbContext.Match.AddRange(inPlay, finished);
        await dbContext.SaveChangesAsync();

        var matches = await service.GetForProcessingAsync(competitionId);

        matches.Select(m => m.MatchID).Should().Equal(finished.MatchID);
    }
}
