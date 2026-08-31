using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class LeagueTableService : ILeagueTableService
{
    private readonly IGenericDbContext _dbContext;
    private readonly IAvatarService _avatarService;

    public LeagueTableService(IGenericDbContext dbContext, IAvatarService avatarService)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
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

        var table = await _dbContext.CallStoredProcedureAsync<LeagueTableItem>("LeagueTableGet", parameters, cancellationToken);

        return table.WithAvatarUrls(_avatarService);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LiveLeagueTableItem>> GetLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
        };

        // Both the standings and the projected standings come back from one procedure. Ranking the
        // projection here instead would mean writing the tie-break order - points, then goal
        // difference, then 3-, 2- and 1-pointers - a second time in a second language, and two
        // copies of a rule are two rules waiting to disagree.
        var table = await _dbContext.CallStoredProcedureAsync<LiveLeagueTableItem>("LiveLeagueTableGet", parameters, cancellationToken);

        return table.WithAvatarUrls(_avatarService);
    }

    private static object ToSqlValue(DateOnly? date)
        => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
}
