using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Imports a competition's full season fixture list from an external data source, replacing the
/// old manual Excel/SQL-script import for competitions with an external competition code configured.
/// </summary>
public interface IFixtureImportService
{
    /// <summary>
    /// Fetches the season fixture list for a competition (using its ExternalApiCompetitionCode and
    /// StartDate.Year to select the source and season), creates any missing TeamCompetition rows and
    /// Match rows, and refines the competition's StartDate/EndDate to the actual fixture date range.
    /// Safe to call repeatedly - already-imported fixtures (matched by ExternalMatchID) are skipped.
    /// </summary>
    /// <param name="competitionId">The competition to import fixtures into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<FixtureImportSummary>> ImportSeasonAsync(Guid competitionId, CancellationToken cancellationToken = default);
}
