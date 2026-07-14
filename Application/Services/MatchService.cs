using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class MatchService : CrudService<Guid, CreateMatchModel, MatchModel, Match>, IMatchService
{
    private readonly IGenericDbContext _dbContext;
    private readonly IApplicationDbContext _appDbContext;

    public MatchService(
        ICrudServiceDependencyAggregate<CreateMatchModel, MatchModel> dependencyAggregate,
        IGenericDbContext dbContext,
        IApplicationDbContext appDbContext
    ) : base(dependencyAggregate)
    {
        _dbContext = dbContext;
        _appDbContext = appDbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserMatchPredictionListItem>> GetUserMatchesForWeekAsync(
        Guid userId,
        Guid competitionId,
        DateTime dateFrom,
        CancellationToken cancellationToken = default)
    {
        var dateTo = dateFrom.AddDays(7).AddMilliseconds(-1);

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@DateFrom", SqlDbType.DateTime) { Value = dateFrom },
            new SqlParameter("@DateTo", SqlDbType.DateTime) { Value = dateTo },
        };

        return await _dbContext.CallStoredProcedureAsync<UserMatchPredictionListItem>("UserMatchPredictionListGet", parameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchModel>> GetForAdminAsync(
        Guid competitionId,
        bool includePlayed,
        CancellationToken cancellationToken = default)
    {
        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId && (includePlayed || !m.MatchPlayed))
            .OrderBy(m => m.MatchDateTime)
            .ToListAsync(cancellationToken);

        return matches.Adapt<List<MatchModel>>();
    }

    /// <summary>
    /// Match.MatchID is ValueGeneratedNever with no database-side default (unlike Competition.CompetitionID,
    /// which defaults to NEWID()), so a new id has to be generated here on create.
    /// </summary>
    protected override Match MapToEntity(CreateMatchModel model)
    {
        var entity = base.MapToEntity(model);
        entity.MatchID = Guid.NewGuid();
        return entity;
    }
}
