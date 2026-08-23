using System.Text.Json.Serialization;

namespace Predictathon.Application.Models;

/// <summary>
/// One row of a competition's league table, as returned by the LeagueTableGet stored procedure.
/// Property names match its output columns exactly so CallStoredProcedureAsync's column-to-property
/// mapping picks them up with no extra configuration.
/// </summary>
public class LeagueTableItem
{
    public string Username { get; set; } = string.Empty;

    public Guid UserID { get; set; }

    /// <summary>
    /// Whether the user has uploaded an avatar image. Only used to derive <see cref="AvatarUrl"/>,
    /// so it isn't serialised to clients.
    /// </summary>
    [JsonIgnore]
    public bool ImageUploaded { get; set; }

    /// <summary>
    /// The user's small avatar image URL, or null when they haven't uploaded one (clients fall back
    /// to their initial). Populated by the service from <see cref="ImageUploaded"/>, not by the
    /// stored procedure.
    /// </summary>
    public string? AvatarUrl { get; set; }

    public int LeaguePosition { get; set; }

    /// <summary>
    /// Only populated when a comparison date was supplied to the procedure; null otherwise.
    /// </summary>
    public int? PreviousLeaguePosition { get; set; }

    public int Score { get; set; }

    public decimal AverageGoalDifference { get; set; }

    public int ThreePointers { get; set; }

    public int TwoPointers { get; set; }

    public int OnePointers { get; set; }

    public int NoPointers { get; set; }

    /// <summary>
    /// Number of played matches the user didn't submit a prediction for.
    /// </summary>
    public int NoPredictions { get; set; }
}
