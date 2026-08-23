using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// The server-side catalogue of every reaction the app can serve an image for: the vendored
/// Twemoji SVGs (addressed by Unicode codepoint) plus Predictathon's own custom reactions.
///
/// This is the single source of truth for reaction images. Clients store and send only an
/// identity - never a URL - and the server resolves it to a filename, so nothing environment-
/// coupled is ever persisted and no client can point a reaction at an arbitrary image.
/// </summary>
public interface IReactionCatalogue
{
    /// <summary>
    /// Every custom (non-Unicode) reaction, in manifest order, for the client's emoji picker.
    /// </summary>
    IReadOnlyList<CustomReactionModel> GetCustomReactions();

    /// <summary>
    /// Reduces a reaction identity to the one spelling this app stores, or null if the identity
    /// is unknown.
    ///
    /// The same emoji reaches us under more than one spelling: the legacy site stored Twemoji's
    /// filename form (<c>u:2764</c>) while emoji-mart hands the picker a padded, FE0F-retaining
    /// form (<c>u:2764-fe0f</c>). Both resolve to the same image, but reactions group and toggle
    /// on the identity <em>string</em> - so without reducing them to one spelling first, the same
    /// emoji shows as two pills that can't toggle each other.
    ///
    /// The canonical form is <c>u:</c> plus the resolved Twemoji filename stem, because that's
    /// derived from the assets we vendor rather than from a third-party dataset that renames
    /// between versions. Custom reactions are already canonical.
    /// </summary>
    /// <param name="reactionId">A namespaced identity, in any accepted spelling.</param>
    string? Canonicalise(string reactionId);

    /// <summary>
    /// Resolves a reaction identity to the filename it's served under on the <c>/reactions</c>
    /// mount, or null if the identity is unknown or has no image on disk.
    /// </summary>
    /// <param name="reactionId">
    /// A namespaced identity: <c>u:{unified}</c> for a standard emoji (where <c>unified</c> is the
    /// hyphen-separated codepoint sequence, e.g. <c>2764-fe0f</c>), or <c>c:{id}</c> for a custom
    /// reaction (e.g. <c>c:ludo</c>).
    /// </param>
    string? ResolveImageFile(string reactionId);
}
