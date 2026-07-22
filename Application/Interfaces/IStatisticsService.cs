using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IStatisticsService
{
    /// <summary>
    /// Gets every all-time (not competition-scoped) statistics widget.
    /// </summary>
    Task<AllTimeStatisticsModel> GetAllTimeStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every current-competition-scoped statistics widget, personalised to the given user
    /// (predictable-matches results include that user's own prediction alongside the average).
    /// </summary>
    Task<CurrentCompetitionStatisticsModel> GetCurrentCompetitionStatisticsAsync(Guid competitionId, Guid userId, CancellationToken cancellationToken = default);
}
