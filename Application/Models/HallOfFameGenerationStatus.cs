namespace Predictathon.Application.Models;

/// <summary>
/// Whether a competition is currently eligible to have its Hall of Fame entry auto-generated,
/// for driving the admin UI's button state without attempting (and failing) the generation itself.
/// </summary>
public class HallOfFameGenerationStatus
{
    /// <summary>
    /// True once every match in the competition has been played.
    /// </summary>
    public bool AllMatchesPlayed { get; set; }

    /// <summary>
    /// True if the competition already has a Hall of Fame entry.
    /// </summary>
    public bool AlreadyGenerated { get; set; }
}
