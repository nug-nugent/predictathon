namespace Predictathon.Application.Models;

/// <summary>
/// Model used when creating a match.
/// </summary>
public class CreateMatchModel
{
    public Guid CompetitionID { get; set; }

    public DateTime MatchDateTime { get; set; }

    public Guid? HomeTeamID { get; set; }

    public Guid? AwayTeamID { get; set; }

    // Placeholder team name (e.g. "Winner of Group A") used when the actual team isn't known yet.
    // Only meaningful when the corresponding TeamID is null.
    public string? HomeTeamTBC { get; set; }

    public string? AwayTeamTBC { get; set; }

    public string? Description { get; set; }

    public int? HomeTeamGoals { get; set; }

    public int? AwayTeamGoals { get; set; }

    public bool NeutralGround { get; set; }

    public bool Knockout { get; set; }

    public bool MatchPlayed { get; set; }
}

// Full model including the generated identifier.
public class MatchModel : CreateMatchModel
{
    public Guid MatchID { get; set; }
}
