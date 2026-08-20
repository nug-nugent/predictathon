using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IStatisticsService
{
    /// <summary>
    /// Gets every all-time (not competition-scoped) statistics widget.
    /// </summary>
    Task<AllTimeStatisticsModel> GetAllTimeStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the all-time league table - one row per user across every competition they've ever
    /// been registered for, ranked the same way as a single competition's league table.
    /// </summary>
    Task<IReadOnlyList<LeagueTableItem>> GetAllTimeLeagueTableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every current-competition-scoped statistics widget, personalised to the given user
    /// (predictable-matches results include that user's own prediction alongside the average).
    /// </summary>
    Task<CurrentCompetitionStatisticsModel> GetCurrentCompetitionStatisticsAsync(Guid competitionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the predictions that beat the average prediction score for their match, best first,
    /// optionally restricted to matches within a date range.
    /// </summary>
    /// <param name="competitionId">The competition to search within.</param>
    /// <param name="dateFrom">Only include matches played on or after this date.</param>
    /// <param name="dateTo">Only include matches played on or before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<BestPredictionListItem>> GetBestPredictionsAsync(Guid competitionId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken cancellationToken = default);
}
