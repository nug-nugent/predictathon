namespace Predictathon.Application.Models;

public class AddReactionRequest
{
    /// <summary>
    /// Namespaced reaction identity - <c>u:{unified}</c> or <c>c:{id}</c>. Rejected if the server's
    /// reaction catalogue can't resolve it to an image.
    /// </summary>
    public string ReactionId { get; set; } = "";

    /// <summary>
    /// Human-readable label for display/alt text. Not an identity, and not trusted for anything else.
    /// </summary>
    public string ReactionName { get; set; } = "";
}
