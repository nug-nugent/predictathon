namespace Predictathon.Application.Models;

/// <summary>
/// One of Predictathon's own custom reactions (as opposed to a standard Unicode emoji), as listed
/// in the server-side <c>custom-reactions.json</c> manifest that sits alongside the image files.
/// </summary>
public class CustomReactionModel
{
    /// <summary>
    /// Stable identifier, stored (namespaced as <c>c:{id}</c>) against every reaction that uses it.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Human-readable label shown in the picker and used as the image's alt text.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Filename under the <c>/reactions</c> static mount, e.g. <c>ludo.png</c>.
    /// </summary>
    public string ImageFile { get; set; } = "";
}
