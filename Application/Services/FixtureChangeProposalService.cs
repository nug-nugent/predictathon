using FluentResults;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class FixtureChangeProposalService : IFixtureChangeProposalService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IExternalMatchDataService _externalMatchDataService;

    public FixtureChangeProposalService(IApplicationDbContext dbContext, IExternalMatchDataService externalMatchDataService)
    {
        _dbContext = dbContext;
        _externalMatchDataService = externalMatchDataService;
    }

    /// <inheritdoc />
    public async Task<Result> DetectChangesAsync(CancellationToken cancellationToken = default)
    {
        // Skip competitions that have already finished - nothing left to reschedule, and no point
        // spending API calls (or free-tier rate limit) checking a season that's over.
        var today = DateOnly.FromDateTime(UkClock.Now);

        var competitions = await _dbContext.Competition
            .Where(c => c.ExternalApiCompetitionCode != null && c.EndDate >= today)
            .ToListAsync(cancellationToken);

        foreach (var competition in competitions)
        {
            var fixtures = await _externalMatchDataService.GetFixturesAsync(
                competition.ExternalApiCompetitionCode!,
                competition.StartDate.Year,
                cancellationToken);

            var fixturesByExternalId = fixtures.ToDictionary(f => f.ExternalMatchID);

            var matches = await _dbContext.Match
                .Where(m => m.CompetitionID == competition.CompetitionID && m.ExternalMatchID != null)
                .ToListAsync(cancellationToken);

            // Batch-fetched once per competition rather than queried per match inside the loop
            // below - a full season is ~380 matches, and this used to mean 380 individual
            // round trips (an N+1) on every sync run.
            var matchIds = matches.Select(m => m.MatchID).ToList();
            var pendingProposalsByMatchId = await _dbContext.FixtureChangeProposal
                .Where(p => matchIds.Contains(p.MatchID) && p.Status == FixtureChangeProposalStatuses.Pending)
                .ToDictionaryAsync(p => p.MatchID, cancellationToken);

            foreach (var match in matches)
            {
                if (!fixturesByExternalId.TryGetValue(match.ExternalMatchID!.Value, out var fixture))
                {
                    continue;
                }

                pendingProposalsByMatchId.TryGetValue(match.MatchID, out var pendingProposal);
                await ReconcileMatchAsync(match, fixture, pendingProposal, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>
    /// Compares one match's stored kickoff against the externally-reported kickoff, and upserts,
    /// refreshes, or auto-dismisses its pending proposal as needed. <paramref name="pendingProposal"/>
    /// is looked up by the caller (batched across the whole competition) rather than queried here.
    /// </summary>
    private async Task ReconcileMatchAsync(Match match, ExternalFixture fixture, FixtureChangeProposal? pendingProposal, CancellationToken cancellationToken)
    {
        var externalKickoff = UkClock.ToUkLocal(fixture.KickoffUtc);
        var matchesStoredKickoff = externalKickoff == match.MatchDateTime;

        if (pendingProposal is null)
        {
            if (matchesStoredKickoff)
            {
                return;
            }

            await _dbContext.AddAsync(new FixtureChangeProposal
            {
                MatchID = match.MatchID,
                PreviousMatchDateTime = match.MatchDateTime,
                ProposedMatchDateTime = externalKickoff,
                Status = FixtureChangeProposalStatuses.Pending,
            }, cancellationToken);

            return;
        }

        if (matchesStoredKickoff)
        {
            // The external kickoff has reverted back to what's already stored - the pending
            // proposal is now stale, so clear it rather than leaving a no-op change for review.
            pendingProposal.Status = FixtureChangeProposalStatuses.Dismissed;
            pendingProposal.ResolvedAtUtc = DateTime.UtcNow;
            _dbContext.Update(pendingProposal);
            return;
        }

        if (pendingProposal.ProposedMatchDateTime != externalKickoff)
        {
            // The fixture has moved again since the proposal was first raised - refresh it in
            // place instead of raising a second pending proposal for the same match.
            pendingProposal.ProposedMatchDateTime = externalKickoff;
            _dbContext.Update(pendingProposal);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FixtureChangeProposalModel>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var proposals = await _dbContext.FixtureChangeProposal
            .Where(p => p.Status == FixtureChangeProposalStatuses.Pending)
            .Include(p => p.Match).ThenInclude(m => m.HomeTeam)
            .Include(p => p.Match).ThenInclude(m => m.AwayTeam)
            .OrderBy(p => p.ProposedMatchDateTime)
            .ToListAsync(cancellationToken);

        return proposals
            .Select(p => new FixtureChangeProposalModel
            {
                FixtureChangeProposalID = p.FixtureChangeProposalID,
                MatchID = p.MatchID,
                HomeTeamName = p.Match.HomeTeam?.TeamName ?? p.Match.HomeTeamTBC ?? "TBC",
                AwayTeamName = p.Match.AwayTeam?.TeamName ?? p.Match.AwayTeamTBC ?? "TBC",
                PreviousMatchDateTime = p.PreviousMatchDateTime,
                ProposedMatchDateTime = p.ProposedMatchDateTime,
                DetectedAtUtc = p.DetectedAtUtc,
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmAsync(int proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.FixtureChangeProposal
            .FirstOrDefaultAsync(p => p.FixtureChangeProposalID == proposalId && p.Status == FixtureChangeProposalStatuses.Pending, cancellationToken);

        if (proposal is null)
        {
            return Result.Fail(new NotFoundError("The fixture change proposal could not be found."));
        }

        var match = await _dbContext.Match.FirstOrDefaultAsync(m => m.MatchID == proposal.MatchID, cancellationToken);
        if (match is null)
        {
            return Result.Fail(new NotFoundError("The match could not be found."));
        }

        match.MatchDateTime = proposal.ProposedMatchDateTime;
        proposal.Status = FixtureChangeProposalStatuses.Confirmed;
        proposal.ResolvedAtUtc = DateTime.UtcNow;

        _dbContext.Update(match);
        _dbContext.Update(proposal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> DismissAsync(int proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.FixtureChangeProposal
            .FirstOrDefaultAsync(p => p.FixtureChangeProposalID == proposalId && p.Status == FixtureChangeProposalStatuses.Pending, cancellationToken);

        if (proposal is null)
        {
            return Result.Fail(new NotFoundError("The fixture change proposal could not be found."));
        }

        proposal.Status = FixtureChangeProposalStatuses.Dismissed;
        proposal.ResolvedAtUtc = DateTime.UtcNow;

        _dbContext.Update(proposal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
