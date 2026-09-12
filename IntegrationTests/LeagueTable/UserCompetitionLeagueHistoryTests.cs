using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using System.Data;

namespace Predictathon.IntegrationTests.LeagueTable;

/// <summary>
/// Exercises dbo.UserCompetitionLeagueHistorySet, the nightly snapshot behind the Profile page's
/// position-over-time chart. Its rows are only worth anything if they mean the same thing a
/// LeagueTableGet row means, so what's covered here is the two places the two queries could count
/// different things: who gets ranked, and which matches count.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class UserCompetitionLeagueHistoryTests
{
    private readonly DatabaseFixture _fixture;

    public UserCompetitionLeagueHistoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UserCompetitionLeagueHistorySet_RanksTheCompetitionsRegistrantsOnly()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = NewCompetition();
        dbContext.Competition.Add(competition);

        // Nobody predicts, so all three are level on nothing and the ranking falls through to the
        // username tie-break - which puts the outsider between the two registrants, exactly where
        // they'd steal a position if the ranking ran across every user on the site.
        var suffix = $"{Guid.NewGuid():N}";
        var first = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"aaa-{suffix}" };
        var outsider = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"mmm-{suffix}" };
        var second = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"zzz-{suffix}" };
        dbContext.Users.AddRange(first, outsider, second);

        var firstRegistration = new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = first.Id, CompetitionID = competition.CompetitionID };
        var secondRegistration = new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = second.Id, CompetitionID = competition.CompetitionID };
        dbContext.UserCompetition.AddRange(firstRegistration, secondRegistration);

        await dbContext.SaveChangesAsync();

        var home = NewTeam("HOM");
        var away = NewTeam("AWY");
        dbContext.Team.AddRange(home, away);

        var match = NewMatch(competition.CompetitionID, home.TeamID, away.TeamID, DateTime.UtcNow.AddDays(-1), played: true);
        dbContext.Match.Add(match);

        await dbContext.SaveChangesAsync();

        try
        {
            await SnapshotAsync(dbContext, competition.CompetitionID);

            var snapshot = await SnapshotRowsAsync(dbContext, competition.CompetitionID);

            snapshot.Should().HaveCount(2);
            snapshot.Single(h => h.UserCompetitionID == firstRegistration.UserCompetitionID).LeaguePosition.Should().Be(1);
            snapshot.Single(h => h.UserCompetitionID == secondRegistration.UserCompetitionID).LeaguePosition.Should().Be(2);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [first.Id, outsider.Id, second.Id], [home.TeamID, away.TeamID]);
        }
    }

    [Fact]
    public async Task UserCompetitionLeagueHistorySet_CountsPlayedMatchesOnly()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = NewCompetition();
        dbContext.Competition.Add(competition);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);

        var registration = new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = user.Id, CompetitionID = competition.CompetitionID };
        dbContext.UserCompetition.Add(registration);

        await dbContext.SaveChangesAsync();

        var home = NewTeam("HOM");
        var away = NewTeam("AWY");
        dbContext.Team.AddRange(home, away);

        var playedMatch = NewMatch(competition.CompetitionID, home.TeamID, away.TeamID, DateTime.UtcNow.AddDays(-2), played: true);
        var unplayedMatch = NewMatch(competition.CompetitionID, home.TeamID, away.TeamID, DateTime.UtcNow.AddDays(-1), played: false);
        dbContext.Match.AddRange(playedMatch, unplayedMatch);

        // A prediction can carry a score on a match that hasn't been processed yet - a result that's
        // been entered but not confirmed, say. LeagueTableGet ignores those points until the match is
        // played, so a snapshot that counted them would read as a different table entirely.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = playedMatch.MatchID, UserID = user.Id, HomeTeamGoals = 2, AwayTeamGoals = 0, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = unplayedMatch.MatchID, UserID = user.Id, HomeTeamGoals = 2, AwayTeamGoals = 0, Score = 3, GoalDifference = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            await SnapshotAsync(dbContext, competition.CompetitionID);

            var snapshot = await SnapshotRowsAsync(dbContext, competition.CompetitionID);

            snapshot.Single().Score.Should().Be(3);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID,
                [playedMatch.MatchID, unplayedMatch.MatchID],
                [user.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    /// <summary>
    /// Runs the snapshot procedure for today, the way the scheduled task does.
    /// </summary>
    /// <param name="dbContext">The test database context.</param>
    /// <param name="competitionId">The competition to snapshot.</param>
    private static async Task SnapshotAsync(ApplicationDbContext dbContext, Guid competitionId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@Date", SqlDbType.Date) { Value = DateTime.Today },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
        };

        await dbContext.CallStoredProcedureAsync("UserCompetitionLeagueHistorySet", parameters);
    }

    /// <summary>
    /// Reads back the snapshot rows written for a competition.
    /// </summary>
    /// <param name="dbContext">The test database context.</param>
    /// <param name="competitionId">The competition whose snapshot to read.</param>
    private static async Task<IReadOnlyList<UserCompetitionLeagueHistory>> SnapshotRowsAsync(ApplicationDbContext dbContext, Guid competitionId)
    {
        var userCompetitionIds = await dbContext.UserCompetition
            .Where(uc => uc.CompetitionID == competitionId)
            .Select(uc => uc.UserCompetitionID)
            .ToListAsync();

        return await dbContext.UserCompetitionLeagueHistory
            .Where(h => userCompetitionIds.Contains(h.UserCompetitionID))
            .AsNoTracking()
            .ToListAsync();
    }

    private static Competition NewCompetition() => new Competition
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = $"Integration Test {Guid.NewGuid():N}",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
    };

    /// <summary>
    /// Builds a throwaway team for a test fixture.
    /// </summary>
    /// <param name="shortName">The team's short name.</param>
    private static Team NewTeam(string shortName) => new Team
    {
        TeamID = Guid.NewGuid(),
        TeamName = $"{shortName} {Guid.NewGuid():N}",
        ShortName = shortName,
    };

    /// <summary>
    /// Builds a throwaway 2-0 match for a test fixture.
    /// </summary>
    /// <param name="competitionId">The competition the match belongs to.</param>
    /// <param name="homeTeamId">The home team.</param>
    /// <param name="awayTeamId">The away team.</param>
    /// <param name="kickOff">When the match kicks off.</param>
    /// <param name="played">Whether the result has been processed.</param>
    private static Match NewMatch(Guid competitionId, Guid homeTeamId, Guid awayTeamId, DateTime kickOff, bool played) => new Match
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = competitionId,
        MatchDateTime = kickOff,
        HomeTeamID = homeTeamId,
        AwayTeamID = awayTeamId,
        MatchPlayed = played,
        HomeTeamGoals = played ? 2 : null,
        AwayTeamGoals = played ? 0 : null,
    };

    /// <summary>
    /// Removes everything a test created, snapshot rows first.
    /// </summary>
    /// <param name="dbContext">The test database context.</param>
    /// <param name="competitionId">The competition to remove.</param>
    /// <param name="matchIds">Matches to remove, along with their predictions.</param>
    /// <param name="userIds">Users to remove.</param>
    /// <param name="teamIds">Teams to remove.</param>
    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        var userCompetitionIds = await dbContext.UserCompetition
            .Where(uc => uc.CompetitionID == competitionId)
            .Select(uc => uc.UserCompetitionID)
            .ToListAsync();

        // UserCompetitionLeagueHistory has no modelled EF navigation back to UserCompetition, so
        // EF's change tracker has no dependency-graph edge telling it to delete these first -
        // saving here guarantees that ordering rather than relying on it to infer one.
        dbContext.UserCompetitionLeagueHistory.RemoveRange(dbContext.UserCompetitionLeagueHistory.Where(h => userCompetitionIds.Contains(h.UserCompetitionID)));
        await dbContext.SaveChangesAsync();

        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => uc.CompetitionID == competitionId));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
