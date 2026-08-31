namespace Predictathon.Application.Models;

/// <summary>
/// A competition series - the grouping that turns repeated wins in the same competition into a
/// single counted trophy. Reference data, seeded by the post-deployment scripts rather than
/// maintained through the app.
/// </summary>
public class CompetitionSeriesModel
{
    public Guid CompetitionSeriesID { get; set; }

    public string SeriesName { get; set; } = "";

    public string ShortName { get; set; } = "";

    /// <summary>
    /// The lucide icon name the frontend draws for this series, or null to fall back to a trophy.
    /// </summary>
    public string? BadgeIcon { get; set; }

    /// <summary>
    /// The badge's colour as a hex string, or null to fall back to the theme's trophy gold.
    /// </summary>
    public string? BadgeColour { get; set; }

    public int DisplayOrder { get; set; }
}
