using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class CompetitionService : CrudService<Guid, CreateCompetitionModel, CompetitionModel, Competition>, ICompetitionService
{
    private readonly IApplicationDbContext _appDbContext;

    public CompetitionService(
        ICrudServiceDependencyAggregate<CreateCompetitionModel, CompetitionModel> dependencyAggregate,
        IApplicationDbContext appDbContext
    ) : base(dependencyAggregate)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competition>> GetCompetitionListAsync()
    {
        return await _appDbContext.Competition.OrderByDescending(c => c.StartDate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competition>> GetCompetitionListForLoginPageAsync()
    {
        return await _appDbContext.Competition.Where(c => c.RegistrationAvailableOnLoginPage).OrderByDescending(c => c.StartDate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompetitionSeriesModel>> GetCompetitionSeriesListAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.CompetitionSeries
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SeriesName)
            .Select(s => new CompetitionSeriesModel
            {
                CompetitionSeriesID = s.CompetitionSeriesID,
                SeriesName = s.SeriesName,
                ShortName = s.ShortName,
                BadgeIcon = s.BadgeIcon,
                BadgeColour = s.BadgeColour,
                DisplayOrder = s.DisplayOrder,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserCompetitionRegistrationListItem>> GetUserCompetitionRegistrationListAsync(Guid userId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<UserCompetitionRegistrationListItem>("UserCompetitionRegistrationListGet", parameters);

        return results;
    }

    /// <inheritdoc />
    public async Task<Result> SetDefaultCompetitionAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default)
    {
        var registrations = await _appDbContext.UserCompetition
            .Where(uc => uc.UserID == userId)
            .ToListAsync(cancellationToken);

        var target = registrations.FirstOrDefault(uc => uc.CompetitionID == competitionId);
        if (target is null)
        {
            return Result.Fail(new NotFoundError("You are not registered for this competition."));
        }

        foreach (var registration in registrations)
        {
            registration.IsDefaultCompetition = registration.CompetitionID == competitionId;
        }

        _appDbContext.UpdateRange(registrations);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserCompetitionLeagueHistoryItem>> GetUserCompetitionLeagueHistoryAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
        };

        return await _appDbContext.CallStoredProcedureAsync<UserCompetitionLeagueHistoryItem>("UserCompetitionLeagueHistoryListGet", parameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetUserCompetitionLeagueHistoryAsync()
    {
        var competitions = await _appDbContext.Competition.ToListAsync();

        foreach (var comp in competitions)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Date", SqlDbType.Date) { Value = DateTime.Today },
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = comp.CompetitionID }
            };

            await _appDbContext.CallStoredProcedureAsync("UserCompetitionLeagueHistorySet", parameters);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompetitionRealLeagueTableItem>> CompetitionRealLeagueTableGetAsync(Guid competitionId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<CompetitionRealLeagueTableItem>("CompetitionRealLeagueTableGet", parameters);

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompetitionUserLeagueTableItem>> CompetitionUserLeagueTableGetAsync(Guid competitionId, Guid userId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<CompetitionUserLeagueTableItem>("CompetitionUserLeagueTableGet", parameters);

        return results;
    }

    /// <inheritdoc />
    public async Task<IList<DateTime>> GetCompetitionWeeksAsync(Guid competitionId)
    {
        var dates = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchDateTime.Date)
            .Distinct()
            .ToListAsync();

        var weeks = dates.Select(MatchWeekStart)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return weeks;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompetitionWeekSummary>> GetCompetitionWeekSummariesAsync(
        Guid competitionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => new { m.MatchID, m.MatchDateTime })
            .ToListAsync(cancellationToken);

        // Queried separately (rather than via m.Prediction.Any(...)) so this doesn't depend on the
        // Match.Prediction navigation being populated - keeps it working against the unit tests'
        // InMemory context, which strips navigations by default.
        var matchIds = _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchID);

        var predictedMatchIds = (await _appDbContext.Prediction
            .Where(p => p.UserID == userId
                && matchIds.Contains(p.MatchID)
                && p.HomeTeamGoals != null
                && p.AwayTeamGoals != null)
            .Select(p => p.MatchID)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // MatchDateTime is naive UK wall-clock, so compare against UK now rather than server-local.
        var now = UkClock.Now;

        return matches
            .GroupBy(m => MatchWeekStart(m.MatchDateTime.Date))
            .OrderBy(g => g.Key)
            .Select(g => new CompetitionWeekSummary
            {
                WeekStart = g.Key,
                LastMatchDateTime = g.Max(m => m.MatchDateTime),
                OpenUnpredictedCount = g.Count(m => m.MatchDateTime > now && !predictedMatchIds.Contains(m.MatchID)),
            })
            .ToList();
    }

    /// <summary>
    /// Buckets a match date to the Friday its match week starts on. Mirrored client-side by
    /// prediction-service.ts's matchWeekStartDate - keep the two in step.
    /// </summary>
    /// <param name="date">The match date to bucket.</param>
    private static DateTime MatchWeekStart(DateTime date)
    {
        var knownFriday = new DateTime(1990, 1, 5);
        var diff = (int)(date.Date - knownFriday).TotalDays;
        var mod = ((diff % 7) + 7) % 7;

        return date.Date.AddDays(-mod);
    }

    /// <inheritdoc />
    public async Task<PublicStatsModel> GetPublicStatsAsync(CancellationToken cancellationToken = default)
    {
        return new PublicStatsModel
        {
            PredictionsMadeCount = await _appDbContext.Prediction.CountAsync(cancellationToken),
            CompletedCompetitionsCount = await _appDbContext.HallOfFame.CountAsync(cancellationToken),
        };
    }
}
