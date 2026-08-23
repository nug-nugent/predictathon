namespace Predictathon.Application.Models;

/// <summary>
/// A single reaction on a message.
///
/// <see cref="ReactionId"/> is the identity: reactions group, toggle and de-duplicate on it, and
/// the server resolves it to <see cref="ImageFile"/> via <see cref="Interfaces.IReactionCatalogue"/>.
/// <see cref="ReactionName"/> is a display label only.
/// </summary>
public class MessageReactionModel
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    /// <summary>
    /// Namespaced reaction identity - <c>u:{unified}</c> or <c>c:{id}</c>.
    /// </summary>
    public string ReactionId { get; set; } = "";

    /// <summary>
    /// Human-readable label, used for alt text and the picker. Not an identity.
    /// </summary>
    public string ReactionName { get; set; } = "";

    /// <summary>
    /// Filename under the <c>/reactions</c> static mount. The client builds the full URL itself,
    /// so no environment-specific URL is ever stored or sent.
    /// </summary>
    public string ImageFile { get; set; } = "";
}
