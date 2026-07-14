using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IHallOfFameService
{
    /// <summary>
    /// Gets every Hall of Fame entry, most recently concluded competition first.
    /// </summary>
    Task<IReadOnlyList<HallOfFameListItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
