using FluentAssertions;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class PredictionServiceTests
{
    private static DomainEntities.Match MakeMatch(DateTime matchDateTime) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = Guid.NewGuid(),
        MatchDateTime = matchDateTime,
    };

    [Fact]
    public async Task SavePredictionAsync_MatchNotFound_ReturnsNotFound()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var service = new PredictionService(dbContext);

        var result = await service.SavePredictionAsync(Guid.NewGuid(), Guid.NewGuid(), 1, 0);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task SavePredictionAsync_MatchWellInFuture_Succeeds()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddHours(1));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.SavePredictionAsync(match.MatchID, Guid.NewGuid(), 2, 1);

        result.IsSuccess.Should().BeTrue();
        var savedPrediction = dbContext.Prediction.Single();
        savedPrediction.HomeTeamGoals.Should().Be(2);
        savedPrediction.AwayTeamGoals.Should().Be(1);
    }

    [Fact]
    public async Task SavePredictionAsync_MatchAlreadyKickedOff_ReturnsConflict()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddMinutes(-10));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.SavePredictionAsync(match.MatchID, Guid.NewGuid(), 1, 1);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task SavePredictionAsync_WithinTwoMinuteCutoff_ReturnsConflict()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddSeconds(30));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.SavePredictionAsync(match.MatchID, Guid.NewGuid(), 1, 1);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task SavePredictionAsync_ExistingPrediction_UpdatesInPlace()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddHours(1));
        dbContext.Match.Add(match);
        var userId = Guid.NewGuid();
        var existing = new DomainEntities.Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = userId,
            HomeTeamGoals = 0,
            AwayTeamGoals = 0,
        };
        dbContext.Prediction.Add(existing);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.SavePredictionAsync(match.MatchID, userId, 3, 2);

        result.IsSuccess.Should().BeTrue();
        dbContext.Prediction.Should().ContainSingle();
        var updated = dbContext.Prediction.Single();
        updated.PredictionID.Should().Be(existing.PredictionID);
        updated.HomeTeamGoals.Should().Be(3);
        updated.AwayTeamGoals.Should().Be(2);
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_MatchNotFound_ReturnsNotFound()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var service = new PredictionService(dbContext);

        var result = await service.GetMatchPredictionsAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_BeforeRevealCutoff_ReturnsConflict()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddHours(1));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.GetMatchPredictionsAsync(match.MatchID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task GetMatchPredictionsAsync_AfterRevealCutoff_Succeeds()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var match = MakeMatch(UkClock.Now.AddMinutes(-10));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var service = new PredictionService(dbContext);

        var result = await service.GetMatchPredictionsAsync(match.MatchID);

        result.IsSuccess.Should().BeTrue();
    }
}
