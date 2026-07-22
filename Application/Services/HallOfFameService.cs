using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Services;

[ScopedService]
public class HallOfFameService : IHallOfFameService
{
    private readonly IGenericDbContext _dbContext;

    public HallOfFameService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HallOfFameListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CallStoredProcedureAsync<HallOfFameListItem>("HallOfFameListGet", cancellationToken: cancellationToken);
    }
}
