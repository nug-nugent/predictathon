using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class LeagueTableService : ILeagueTableService
{
    private readonly IGenericDbContext _dbContext;

    public LeagueTableService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        DateOnly? dateForComparison = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@DateFrom", SqlDbType.Date) { Value = ToSqlValue(dateFrom) },
            new SqlParameter("@DateTo", SqlDbType.Date) { Value = ToSqlValue(dateTo) },
            new SqlParameter("@DateForComparison", SqlDbType.Date) { Value = ToSqlValue(dateForComparison) },
        };

        return await _dbContext.CallStoredProcedureAsync<LeagueTableItem>("LeagueTableGet", parameters, cancellationToken);
    }

    private static object ToSqlValue(DateOnly? date)
        => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
}
