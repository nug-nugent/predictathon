using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.Trophies;

/// <summary>
/// Exercises dbo.UserTrophiesGet, which turns Hall of Fame rows into the trophies shown on a
/// profile. All of the interesting behaviour - collapsing repeated wins in one series into a
/// counted trophy while keeping series-less one-offs apart, and resolving a series from the Hall
/// of Fame row ahead of the competition - lives in that GROUP BY, so it is covered here against a
/// real SQL Server instance rather than through the InMemory provider.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class UserTrophiesTests
{
    private readonly DatabaseFixture _fixture;

    public UserTrophiesTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetForUserAsync_GroupsSeriesWinsAndKeepsOneOffsApart()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var series = new CompetitionSeries
        {
            CompetitionSeriesID = Guid.NewGuid(),
            SeriesName = $"Series {Guid.NewGuid():N}",
            ShortName = "TST",
            BadgeIcon = "crown",
            BadgeColour = "#123456",
            DisplayOrder = 5,
        };
        dbContext.CompetitionSeries.Add(series);

        var winner = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"winner-{Guid.NewGuid():N}" };
        var runnerUp = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"runner-up-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(winner, runnerUp);

        // A competition carrying the series itself, to exercise resolution through the competition
        // rather than off the Hall of Fame row.
        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            CompetitionSeriesID = series.CompetitionSeriesID,
        };
        dbContext.Competition.Add(competition);

        // HallOfFame has no navigation property for its winner/runner-up columns, so EF can't see
        // that those rows depend on the users and won't order the inserts for us - the users have
        // to be in the database before the Hall of Fame rows referencing them.
        await dbContext.SaveChangesAsync();

        var entries = new[]
        {
            // Two wins in the same series, reached by the two different routes - one through the
            // competition, one off the Hall of Fame row (as the oldest entries must).
            new HallOfFame
            {
                HallOfFameID = Guid.NewGuid(),
                CompetitionID = competition.CompetitionID,
                CompetitionName = competition.CompetitionName,
                WinnerUserID = winner.Id,
                SecondPlaceUserID = runnerUp.Id,
                EndDate = new DateOnly(2014, 7, 13),
            },
            new HallOfFame
            {
                HallOfFameID = Guid.NewGuid(),
                CompetitionName = "Ancient Cup",
                CompetitionSeriesID = series.CompetitionSeriesID,
                WinnerUserID = winner.Id,
                EndDate = new DateOnly(1998, 7, 12),
            },
            // A win in no series at all: its own trophy, named after the competition.
            new HallOfFame
            {
                HallOfFameID = Guid.NewGuid(),
                CompetitionName = "Millennium Shield",
                WinnerUserID = winner.Id,
                EndDate = new DateOnly(2000, 12, 31),
            },
            // Second place earns nothing - trophies are wins only.
            new HallOfFame
            {
                HallOfFameID = Guid.NewGuid(),
                CompetitionName = "Consolation Cup",
                WinnerUserID = winner.Id,
                SecondPlaceUserID = runnerUp.Id,
                EndDate = new DateOnly(2005, 5, 5),
            },
        };
        dbContext.HallOfFame.AddRange(entries);

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new TrophyService(dbContext);

            var trophies = await service.GetForUserAsync(winner.Id);

            var seriesTrophy = trophies.Single(t => t.CompetitionSeriesID == series.CompetitionSeriesID);
            seriesTrophy.Name.Should().Be(series.SeriesName, "a series win is named after the series, not the competition");
            seriesTrophy.WinCount.Should().Be(2, "both wins belong to the same series and collapse into one trophy");
            seriesTrophy.Years.Should().Be("1998, 2014", "years are listed oldest first");
            seriesTrophy.MostRecentWin.Should().Be(new DateOnly(2014, 7, 13));
            seriesTrophy.BadgeIcon.Should().Be("crown");
            seriesTrophy.BadgeColour.Should().Be("#123456");

            var oneOffs = trophies.Where(t => t.CompetitionSeriesID is null).ToList();
            oneOffs.Should().HaveCount(2, "series-less wins stay separate trophies rather than sharing one");
            oneOffs.Select(t => t.Name).Should().BeEquivalentTo(["Millennium Shield", "Consolation Cup"]);
            oneOffs.Should().OnlyContain(t => t.WinCount == 1);

            trophies.Should().HaveCount(3);
            trophies[0].CompetitionSeriesID.Should().Be(series.CompetitionSeriesID, "series trophies sort ahead of series-less one-offs");

            // The runner-up placed second twice and won nothing, so has no trophies at all.
            (await service.GetForUserAsync(runnerUp.Id)).Should().BeEmpty("second place is not a win");
        }
        finally
        {
            await CleanUpAsync(dbContext, [.. entries.Select(e => e.HallOfFameID)],
                [competition.CompetitionID], [winner.Id, runnerUp.Id], series.CompetitionSeriesID);
        }
    }

    [Fact]
    public async Task GetForUsersAsync_ReturnsOnlyTheRequestedUsers()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var wanted = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"wanted-{Guid.NewGuid():N}" };
        var other = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"other-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(wanted, other);

        // See the note in the test above: the winner columns carry no navigation property, so the
        // users must be saved before the Hall of Fame rows that reference them.
        await dbContext.SaveChangesAsync();

        var entries = new[]
        {
            new HallOfFame { HallOfFameID = Guid.NewGuid(), CompetitionName = "Wanted Cup", WinnerUserID = wanted.Id, EndDate = new DateOnly(2019, 6, 1) },
            new HallOfFame { HallOfFameID = Guid.NewGuid(), CompetitionName = "Other Cup", WinnerUserID = other.Id, EndDate = new DateOnly(2020, 6, 1) },
        };
        dbContext.HallOfFame.AddRange(entries);

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new TrophyService(dbContext);

            var byUser = await service.GetForUsersAsync([wanted.Id]);

            byUser.Should().ContainKey(wanted.Id);
            byUser[wanted.Id].Should().ContainSingle(t => t.Name == "Wanted Cup");
            byUser.Should().NotContainKey(other.Id, "only the requested users' trophies come back");
        }
        finally
        {
            await CleanUpAsync(dbContext, [.. entries.Select(e => e.HallOfFameID)], [], [wanted.Id, other.Id], null);
        }
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<Guid> hallOfFameIds,
        IReadOnlyList<Guid> competitionIds,
        IReadOnlyList<Guid> userIds,
        Guid? competitionSeriesId)
    {
        dbContext.HallOfFame.RemoveRange(dbContext.HallOfFame.Where(h => hallOfFameIds.Contains(h.HallOfFameID)));
        await dbContext.SaveChangesAsync();

        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => competitionIds.Contains(c.CompetitionID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        await dbContext.SaveChangesAsync();

        if (competitionSeriesId is Guid seriesId)
        {
            dbContext.CompetitionSeries.RemoveRange(dbContext.CompetitionSeries.Where(s => s.CompetitionSeriesID == seriesId));
            await dbContext.SaveChangesAsync();
        }
    }
}
