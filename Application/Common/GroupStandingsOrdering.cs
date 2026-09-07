using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Common;

/// <summary>
/// Orders a group's league-table rows, applying whichever tie-break rule the competition uses when
/// teams finish level on points.
///
/// Two rules exist because tournaments genuinely differ. The domestic-league rule ranks everyone by
/// overall goal difference, then goals scored. UEFA's rule - used at the Euros, and now at the World
/// Cup too - instead separates level teams by their record against each other, and only falls back
/// to overall goal difference once that record fails to separate them. The two orderings disagree
/// often enough in a four-team group to be worth getting right rather than approximating.
/// </summary>
public static class GroupStandingsOrdering
{
    /// <summary>
    /// Orders standings best-placed team first and stamps each row's 1-based <see cref="TeamStandingItem.Position"/>.
    /// </summary>
    /// <param name="standings">The group's table rows, already totalled from its matches.</param>
    /// <param name="groupMatches">
    /// The played matches those totals came from, needed to work out head-to-head records. Ignored
    /// when <paramref name="headToHeadTieBreaks"/> is false.
    /// </param>
    /// <param name="headToHeadTieBreaks">
    /// True to separate teams level on points by their record against each other (UEFA's rule),
    /// false to go straight to overall goal difference (the domestic-league rule).
    /// </param>
    public static List<TeamStandingItem> Order(
        IReadOnlyList<TeamStandingItem> standings,
        IReadOnlyList<Match> groupMatches,
        bool headToHeadTieBreaks)
    {
        List<TeamStandingItem> ordered;

        if (headToHeadTieBreaks)
        {
            // Points always come first under either rule; the rules only differ in how they break a
            // tie on points, so resolve each level-on-points block separately.
            ordered = standings
                .GroupBy(s => s.Points)
                .OrderByDescending(block => block.Key)
                .SelectMany(block => ResolveTiedBlock([.. block], groupMatches))
                .ToList();
        }
        else
        {
            ordered = [.. standings.OrderByDescending(s => s.Points).ThenBy(OverallOrdering)];
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].Position = index + 1;
        }

        return ordered;
    }

    /// <summary>
    /// Orders one set of teams that are level on points, following UEFA's criteria: their points,
    /// goal difference and goals scored in the matches among themselves, then - for any teams those
    /// criteria leave level - the same three criteria reapplied to just that smaller set, and
    /// finally overall goal difference and goals scored.
    /// </summary>
    /// <param name="tied">The teams level on points, which may be a subset of a larger tied block.</param>
    /// <param name="groupMatches">The group's played matches, filtered down to these teams here.</param>
    private static List<TeamStandingItem> ResolveTiedBlock(List<TeamStandingItem> tied, IReadOnlyList<Match> groupMatches)
    {
        if (tied.Count == 1)
        {
            return tied;
        }

        var miniTable = BuildMiniTable(tied, groupMatches);

        var separated = tied
            .GroupBy(s => miniTable[s.TeamID])
            .OrderByDescending(block => block.Key.Points)
            .ThenByDescending(block => block.Key.GoalDifference)
            .ThenByDescending(block => block.Key.GoalsFor)
            .ToList();

        // Every team has the same head-to-head record - including the common case of a group where
        // they haven't played each other yet - so reapplying the criteria to a smaller set would
        // only recurse forever on the same teams. Fall through to the overall-record criteria.
        if (separated.Count == 1)
        {
            return [.. tied.OrderBy(OverallOrdering)];
        }

        // Each block here is strictly smaller than the one we came in with, so the recursion is
        // bounded by the number of teams in the group.
        return separated.SelectMany(block => ResolveTiedBlock([.. block], groupMatches)).ToList();
    }

    /// <summary>
    /// Totals the matches played among a set of level teams - and only among themselves - into the
    /// head-to-head record UEFA's first three tie-break criteria compare.
    /// </summary>
    /// <param name="tied">The teams whose head-to-head record is wanted.</param>
    /// <param name="groupMatches">The group's played matches.</param>
    private static Dictionary<Guid, HeadToHeadRecord> BuildMiniTable(List<TeamStandingItem> tied, IReadOnlyList<Match> groupMatches)
    {
        var tiedTeamIds = tied.Select(s => s.TeamID).ToHashSet();
        var records = tied.ToDictionary(s => s.TeamID, _ => new HeadToHeadRecord());

        foreach (var match in groupMatches)
        {
            if (!match.HomeTeamID.HasValue || !match.AwayTeamID.HasValue)
            {
                continue;
            }

            if (!tiedTeamIds.Contains(match.HomeTeamID.Value) || !tiedTeamIds.Contains(match.AwayTeamID.Value))
            {
                continue;
            }

            var homeGoals = match.HomeTeamGoals ?? 0;
            var awayGoals = match.AwayTeamGoals ?? 0;

            records[match.HomeTeamID.Value] = records[match.HomeTeamID.Value].Add(homeGoals, awayGoals);
            records[match.AwayTeamID.Value] = records[match.AwayTeamID.Value].Add(awayGoals, homeGoals);
        }

        return records;
    }

    /// <summary>
    /// The sort key for the criteria that apply to a team's whole group record rather than its
    /// record against particular opponents: goal difference, then goals scored, then team name so
    /// a table that is genuinely undecidable still comes out in a stable order.
    /// </summary>
    /// <param name="standing">The team's table row.</param>
    private static (int NegatedGoalDifference, int NegatedGoalsFor, string TeamName) OverallOrdering(TeamStandingItem standing)
        => (-standing.GoalDifference, -standing.GoalsFor, standing.TeamName);

    /// <summary>
    /// One team's record in the matches against the other teams it is level with.
    /// </summary>
    /// <param name="Points">Points won in those matches.</param>
    /// <param name="GoalsFor">Goals scored in those matches.</param>
    /// <param name="GoalsAgainst">Goals conceded in those matches.</param>
    private readonly record struct HeadToHeadRecord(int Points, int GoalsFor, int GoalsAgainst)
    {
        public int GoalDifference => GoalsFor - GoalsAgainst;

        /// <summary>
        /// Returns this record with one more match added, from the team's own point of view.
        /// </summary>
        /// <param name="goalsFor">Goals the team scored in the match.</param>
        /// <param name="goalsAgainst">Goals the team conceded in the match.</param>
        public HeadToHeadRecord Add(int goalsFor, int goalsAgainst)
        {
            var points = goalsFor > goalsAgainst ? 3 : goalsFor == goalsAgainst ? 1 : 0;

            return new HeadToHeadRecord(Points + points, GoalsFor + goalsFor, GoalsAgainst + goalsAgainst);
        }
    }
}
