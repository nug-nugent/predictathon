using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class TrophyService : ITrophyService
{
    private readonly IGenericDbContext _dbContext;

    public TrophyService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserTrophyModel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId }
        };

        return await _dbContext.CallStoredProcedureAsync<UserTrophyModel>("UserTrophiesGet", parameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, List<UserTrophyModel>>> GetForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, List<UserTrophyModel>>();
        }

        // Every competition ever won produces at most one row per winner, so the whole table is a
        // few hundred rows at most - cheaper to read the lot once and filter here than to pass a
        // table-valued parameter through for what is usually a single page of message authors.
        var all = await _dbContext.CallStoredProcedureAsync<UserTrophyModel>("UserTrophiesGet", cancellationToken: cancellationToken);

        return all
            .Where(t => ids.Contains(t.UserID))
            .GroupBy(t => t.UserID)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
