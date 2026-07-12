using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class MatchService : IMatchService
{
    private readonly IGenericDbContext _dbContext;

    public MatchService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
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
}
