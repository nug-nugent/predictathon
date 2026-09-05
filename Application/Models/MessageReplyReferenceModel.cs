namespace Predictathon.Application.Models;

/// <summary>
/// The quoted stub a reply shows above its own content, identifying the message it replies to.
///
/// Everything needed to render it is denormalised onto the reply itself, so the stub is correct
/// even when the parent sits on an older page the client hasn't loaded - the client never has to
/// go looking for the parent just to draw the quote.
/// </summary>
public class MessageReplyReferenceModel
{
    /// <summary>
    /// The message being replied to, so the client can scroll to it when it is on screen.
    /// </summary>
    public Guid MessageID { get; set; }

    public Guid PostedByUserID { get; set; }

    public string PostedByUsername { get; set; } = "";

    /// <summary>
    /// A short, plain-text excerpt of the parent's content, truncated server-side. Rendered as
    /// plain text rather than markdown: a quoted heading, list or link would otherwise break the
    /// stub's single-line layout.
    /// </summary>
    public string? Snippet { get; set; }

    /// <summary>
    /// The parent's image, if it had one, for the stub's thumbnail. Null when it had none.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Whether the parent was a YouTube post. There is no thumbnail for these - the stub shows a
    /// label instead, so no request ever leaves for youtube.com just to draw a quote.
    /// </summary>
    public bool HasYouTubeVideo { get; set; }
}
