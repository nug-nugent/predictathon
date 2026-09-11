using FluentResults;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

/// <summary>
/// Works out which undecided knockout slots can now be filled in, and fills in the ones an admin
/// picks.
///
/// A tournament's bracket arrives as placeholders - "Winner Group A", "Winner QF1" - and turning
/// each into a team is otherwise thirty edits through a dialog, in the two or three days between a
/// group ending and the tie kicking off. Everything needed to do it is already here: the group
/// tables are computed for the team pages, and a tie's winner is its score. So this reads the
/// placeholders (see <see cref="BracketPlaceholders"/>) and proposes.
///
/// Proposes, and no more. The orderings behind "Winner Group A" are our reading of the competition's
/// tie-break rule, and real tournaments also separate teams on disciplinary points and, in the last
/// resort, by drawing lots - none of which is in this database. An admin confirms every slot.
/// </summary>
[ScopedService]
public class BracketResolutionService : IBracketResolutionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITeamService _teamService;

    public BracketResolutionService(IApplicationDbContext dbContext, ITeamService teamService)
    {
        _dbContext = dbContext;
        _teamService = teamService;
    }

    /// <inheritdoc />
    public async Task<BracketResolutionModel> GetAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var matches = await _dbContext.Match
            .AsNoTracking()
            .Where(m => m.CompetitionID == competitionId)
            .ToListAsync(cancellationToken);

        var teamsById = await _dbContext.Team
            .AsNoTracking()
            .ToDictionaryAsync(t => t.TeamID, cancellationToken);

        var groups = await _teamService.GetGroupStandingsAsync(competitionId, cancellationToken);
        var groupsByName = groups.ToDictionary(g => g.GroupName, StringComparer.OrdinalIgnoreCase);

        // Matches are found by their Description because that is what a placeholder names them by -
        // "Winner QF1" points at the tie described "Quarter-final 1". Two ties sharing a description
        // would make that ambiguous, so neither is offered rather than the wrong one being picked.
        var matchesByDescription = matches
            .Where(m => !string.IsNullOrWhiteSpace(m.Description))
            .GroupBy(m => m.Description!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single(), StringComparer.OrdinalIgnoreCase);

        var rounds = matches
            .Where(m => m.KnockoutRound.HasValue)
            .GroupBy(m => m.KnockoutRound!.Value)
            .OrderByDescending(round => round.Key)
            .Select(round => new BracketResolutionRound
            {
                KnockoutRound = round.Key,
                RoundName = KnockoutRounds.NameOf(round.Key),
                Slots = round
                    .OrderBy(m => m.BracketSlot ?? int.MaxValue)
                    .ThenBy(m => m.MatchDateTime)
                    .SelectMany(match => new[]
                    {
                        DescribeSlot(match, isHome: true, teamsById, groupsByName, matchesByDescription),
                        DescribeSlot(match, isHome: false, teamsById, groupsByName, matchesByDescription),
                    })
                    .ToList(),
            })
            .ToList();

        var allSlots = rounds.SelectMany(r => r.Slots).ToList();

        return new BracketResolutionModel
        {
            Rounds = rounds,
            ReadyCount = allSlots.Count(s => s.Status == BracketSlotStatus.Ready),
            UnsettledCount = allSlots.Count(s => s.Status != BracketSlotStatus.Settled),
        };
    }

    /// <inheritdoc />
    public async Task<Result<BracketResolutionSummary>> ApplyAsync(
        Guid competitionId,
        IReadOnlyList<BracketSlotAssignment> assignments,
        CancellationToken cancellationToken = default)
    {
        if (assignments.Count == 0)
        {
            return Result.Ok(new BracketResolutionSummary { SlotsFilled = 0 });
        }

        var matchIds = assignments.Select(a => a.MatchID).Distinct().ToList();

        var matches = await _dbContext.Match
            .Where(m => m.CompetitionID == competitionId && matchIds.Contains(m.MatchID))
            .ToDictionaryAsync(m => m.MatchID, cancellationToken);

        if (matches.Count != matchIds.Count)
        {
            return Result.Fail<BracketResolutionSummary>(new NotFoundError(
                "One of those matches isn't in this competition."));
        }

        var teamIds = assignments.Select(a => a.TeamID).Distinct().ToList();
        var knownTeamIds = await _dbContext.Team
            .Where(t => teamIds.Contains(t.TeamID))
            .Select(t => t.TeamID)
            .ToListAsync(cancellationToken);

        if (knownTeamIds.Count != teamIds.Count)
        {
            return Result.Fail<BracketResolutionSummary>(new NotFoundError("One of those teams could not be found."));
        }

        var slotsFilled = 0;

        foreach (var assignment in assignments)
        {
            var match = matches[assignment.MatchID];

            // A slot that already has a team is left exactly as it is, rather than overwritten. The
            // screen this comes from was built against a bracket that may have moved on since, and
            // an admin's own correction has to survive someone else pressing the button.
            if (assignment.IsHome && match.HomeTeamID is null)
            {
                match.HomeTeamID = assignment.TeamID;
                slotsFilled++;
            }
            else if (!assignment.IsHome && match.AwayTeamID is null)
            {
                match.AwayTeamID = assignment.TeamID;
                slotsFilled++;
            }
            else
            {
                continue;
            }

            // The placeholder stays. It costs nothing - a decided slot is one with a TeamID, and
            // every reader tests that rather than the text - and it is the only record of where the
            // team came from, which is what makes a mistake here reversible.
            _dbContext.Update(match);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new BracketResolutionSummary { SlotsFilled = slotsFilled });
    }

    /// <summary>
    /// Reads one side of one tie: who is in it, or who could be, or what it is waiting for.
    /// </summary>
    /// <param name="match">The tie.</param>
    /// <param name="isHome">Which side of it.</param>
    /// <param name="teamsById">Every team, for naming.</param>
    /// <param name="groupsByName">The competition's group tables, by group name.</param>
    /// <param name="matchesByDescription">The competition's matches, by their unique descriptions.</param>
    private static BracketResolutionSlot DescribeSlot(
        Match match,
        bool isHome,
        Dictionary<Guid, Team> teamsById,
        Dictionary<string, GroupStandingsModel> groupsByName,
        Dictionary<string, Match> matchesByDescription)
    {
        var currentTeamId = isHome ? match.HomeTeamID : match.AwayTeamID;
        var placeholder = isHome ? match.HomeTeamTBC : match.AwayTeamTBC;

        var slot = new BracketResolutionSlot
        {
            MatchID = match.MatchID,
            MatchDescription = match.Description ?? KnockoutRounds.NameOf(match.KnockoutRound ?? 0),
            IsHome = isHome,
            Placeholder = placeholder,
            CurrentTeamID = currentTeamId,
            CurrentTeamName = currentTeamId is not null && teamsById.TryGetValue(currentTeamId.Value, out var currentTeam)
                ? currentTeam.TeamName
                : null,
        };

        if (currentTeamId is not null)
        {
            slot.Status = BracketSlotStatus.Settled;
            return slot;
        }

        var source = BracketPlaceholders.Parse(placeholder);
        if (source is null)
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = string.IsNullOrWhiteSpace(placeholder)
                ? "No placeholder to work from."
                : $"\"{placeholder}\" isn't a placeholder this can read.";
            return slot;
        }

        return source.Value.Kind == BracketSourceKind.GroupPosition
            ? FromGroup(slot, source.Value, groupsByName, teamsById)
            : FromEarlierTie(slot, source.Value, matchesByDescription, teamsById);
    }

    /// <summary>Fills a slot from a group table, once that group has finished playing.</summary>
    /// <param name="slot">The slot being described.</param>
    /// <param name="source">Which group and which finishing position.</param>
    /// <param name="groupsByName">The competition's group tables, by group name.</param>
    /// <param name="teamsById">Every team, for naming.</param>
    private static BracketResolutionSlot FromGroup(
        BracketResolutionSlot slot,
        BracketSource source,
        Dictionary<string, GroupStandingsModel> groupsByName,
        Dictionary<Guid, Team> teamsById)
    {
        if (!groupsByName.TryGetValue(source.Reference, out var group))
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = $"{source.Reference} isn't a group in this competition.";
            return slot;
        }

        if (!group.IsComplete)
        {
            slot.Status = BracketSlotStatus.Waiting;
            slot.Reason = $"{group.GroupName}: {group.MatchesRemaining} match{(group.MatchesRemaining == 1 ? "" : "es")} left.";
            return slot;
        }

        var standing = group.Standings.FirstOrDefault(s => s.Position == source.GroupPosition);
        if (standing is null)
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = $"{group.GroupName} has no team in position {source.GroupPosition}.";
            return slot;
        }

        slot.Status = BracketSlotStatus.Ready;
        slot.ProposedTeamID = standing.TeamID;
        slot.ProposedTeamName = teamsById.TryGetValue(standing.TeamID, out var team) ? team.TeamName : standing.TeamName;
        return slot;
    }

    /// <summary>Fills a slot from an earlier tie, once that tie has been played and settled.</summary>
    /// <param name="slot">The slot being described.</param>
    /// <param name="source">Which tie, and whether the winner or the loser is wanted.</param>
    /// <param name="matchesByDescription">The competition's matches, by their unique descriptions.</param>
    /// <param name="teamsById">Every team, for naming.</param>
    private static BracketResolutionSlot FromEarlierTie(
        BracketResolutionSlot slot,
        BracketSource source,
        Dictionary<string, Match> matchesByDescription,
        Dictionary<Guid, Team> teamsById)
    {
        if (!matchesByDescription.TryGetValue(source.Reference, out var feeder))
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = $"No single match here is described \"{source.Reference}\".";
            return slot;
        }

        if (!feeder.MatchPlayed)
        {
            slot.Status = BracketSlotStatus.Waiting;
            slot.Reason = $"{source.Reference} hasn't been played.";
            return slot;
        }

        var homeGoals = feeder.HomeTeamGoals ?? 0;
        var awayGoals = feeder.AwayTeamGoals ?? 0;

        // A knockout tie that finished level was settled by extra time or penalties, and neither is
        // in this database - the score is the score at ninety minutes. Nothing here can say who went
        // through, so the admin does. This is the ordinary case in a real tournament, not an oddity.
        if (homeGoals == awayGoals)
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = $"{source.Reference} finished {homeGoals} - {awayGoals}. Who went through?";
            return slot;
        }

        var homeWon = homeGoals > awayGoals;
        var wantWinner = source.Kind == BracketSourceKind.MatchWinner;
        var teamId = homeWon == wantWinner ? feeder.HomeTeamID : feeder.AwayTeamID;

        if (teamId is null)
        {
            slot.Status = BracketSlotStatus.Manual;
            slot.Reason = $"{source.Reference} has been played but its own teams aren't filled in.";
            return slot;
        }

        slot.Status = BracketSlotStatus.Ready;
        slot.ProposedTeamID = teamId;
        slot.ProposedTeamName = teamsById.TryGetValue(teamId.Value, out var team) ? team.TeamName : null;
        return slot;
    }
}
