using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Common;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class CompetitionServiceTests
{
    // 2026-08-21 is a Friday, so it's the start of its own match week and 2026-08-27 (Thursday) is
    // the last day of that same week.
    private static readonly DateTime WeekOneFriday = new(2026, 8, 21);
    private static readonly DateTime WeekTwoFriday = new(2026, 8, 28);

    private static (InMemoryApplicationDbContext DbContext, CompetitionService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateCompetitionModel, CompetitionModel>(dbContext, new Mapper());
        var service = new CompetitionService(dependencyAggregate, dbContext);
        return (dbContext, service);
    }

    private static DomainEntities.Match MakeMatch(Guid competitionId, DateTime kickoff)
    {
        return new DomainEntities.Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = kickoff,
        };
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_BucketsMatchesIntoFridayStartingWeeks()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        dbContext.Match.AddRange(
            // Kick-off exactly on the Friday the week starts.
            MakeMatch(competitionId, WeekOneFriday.AddHours(20)),
            // Saturday and the following Thursday both still belong to that same week.
            MakeMatch(competitionId, WeekOneFriday.AddDays(1).AddHours(15)),
            MakeMatch(competitionId, WeekOneFriday.AddDays(6).AddHours(19)),
            // The next Friday opens a new week.
            MakeMatch(competitionId, WeekTwoFriday.AddHours(20)));
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, Guid.NewGuid());

        summaries.Select(s => s.WeekStart).Should().Equal(WeekOneFriday, WeekTwoFriday);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_ReturnsWeeksInAscendingOrder()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        dbContext.Match.AddRange(
            MakeMatch(competitionId, WeekTwoFriday.AddHours(20)),
            MakeMatch(competitionId, WeekOneFriday.AddHours(20)));
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, Guid.NewGuid());

        summaries.Select(s => s.WeekStart).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_LastMatchDateTimeIsTheLatestKickoffInTheWeek()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var latest = WeekOneFriday.AddDays(2).AddHours(16);

        dbContext.Match.AddRange(
            MakeMatch(competitionId, WeekOneFriday.AddDays(1).AddHours(15)),
            MakeMatch(competitionId, latest),
            MakeMatch(competitionId, WeekOneFriday.AddDays(1).AddHours(17)));
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, Guid.NewGuid());

        summaries.Should().ContainSingle().Which.LastMatchDateTime.Should().Be(latest);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_CountsOnlyUnpredictedMatchesStillInTheFuture()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = UkClock.Now;

        var upcomingUnpredicted = MakeMatch(competitionId, now.AddDays(1));
        var upcomingPredicted = MakeMatch(competitionId, now.AddDays(1).AddHours(2));
        var pastUnpredicted = MakeMatch(competitionId, now.AddHours(-3));
        dbContext.Match.AddRange(upcomingUnpredicted, upcomingPredicted, pastUnpredicted);

        dbContext.Prediction.Add(new DomainEntities.Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = upcomingPredicted.MatchID,
            UserID = userId,
            HomeTeamGoals = 2,
            AwayTeamGoals = 1,
        });
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, userId);

        summaries.Sum(s => s.OpenUnpredictedCount).Should().Be(1);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_IgnoresOtherUsersPredictions()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var match = MakeMatch(competitionId, UkClock.Now.AddDays(1));
        dbContext.Match.Add(match);
        dbContext.Prediction.Add(new DomainEntities.Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = Guid.NewGuid(),
            HomeTeamGoals = 0,
            AwayTeamGoals = 0,
        });
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, userId);

        summaries.Should().ContainSingle().Which.OpenUnpredictedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_TreatsAPredictionWithMissingGoalsAsUnpredicted()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var match = MakeMatch(competitionId, UkClock.Now.AddDays(1));
        dbContext.Match.Add(match);

        // A half-populated row renders as an empty, still-predictable match in MatchList, so it
        // mustn't count as predicted here either.
        dbContext.Prediction.Add(new DomainEntities.Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = userId,
            HomeTeamGoals = 1,
            AwayTeamGoals = null,
        });
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, userId);

        summaries.Should().ContainSingle().Which.OpenUnpredictedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_IgnoresOtherCompetitionsMatches()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();

        dbContext.Match.AddRange(
            MakeMatch(competitionId, WeekOneFriday.AddHours(20)),
            MakeMatch(Guid.NewGuid(), WeekTwoFriday.AddHours(20)));
        await dbContext.SaveChangesAsync();

        var summaries = await service.GetCompetitionWeekSummariesAsync(competitionId, Guid.NewGuid());

        summaries.Select(s => s.WeekStart).Should().Equal(WeekOneFriday);
    }

    [Fact]
    public async Task GetCompetitionWeekSummariesAsync_ReturnsEmptyForACompetitionWithNoMatches()
    {
        var (_, service) = MakeService();

        var summaries = await service.GetCompetitionWeekSummariesAsync(Guid.NewGuid(), Guid.NewGuid());

        summaries.Should().BeEmpty();
    }
}
