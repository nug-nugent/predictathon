namespace Predictathon.Application.Models;

/// <summary>
/// One point in a user's league-position history for a competition, as returned by the
/// UserCompetitionLeagueHistoryListGet stored procedure, ordered by date ascending.
/// </summary>
public class UserCompetitionLeagueHistoryItem
{
    public string Username { get; set; } = "";

    public DateTime Date { get; set; }

    public int Score { get; set; }

    public int LeaguePosition { get; set; }
}
