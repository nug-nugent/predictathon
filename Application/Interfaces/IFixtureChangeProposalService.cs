using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Detects and manages Premier League fixture reschedules against an external data source. Bespoke
/// rather than an <see cref="Base.ICrudService{TPrimaryKey, TCreateModel, TEditModel, TEntity}"/> since
/// the operations here are domain actions (detect/confirm/dismiss), not plain CRUD.
/// </summary>
public interface IFixtureChangeProposalService
{
    /// <summary>
    /// Fetches current fixtures for every competition with an external competition code configured,
    /// and upserts pending proposals for any matches whose kickoff has changed. Safe to call
    /// repeatedly - existing pending proposals are refreshed rather than duplicated, and a pending
    /// proposal is auto-dismissed if the external kickoff time reverts back to what's already stored.
    /// </summary>
    Task<Result> DetectChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every pending fixture-change proposal, earliest proposed kickoff first.
    /// </summary>
    Task<IReadOnlyList<FixtureChangeProposalModel>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a pending proposal's new kickoff time to its match and marks the proposal Confirmed.
    /// </summary>
    Task<Result> ConfirmAsync(int proposalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a pending proposal Dismissed without changing the match.
    /// </summary>
    Task<Result> DismissAsync(int proposalId, CancellationToken cancellationToken = default);
}
