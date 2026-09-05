namespace Predictathon.Application.Models;

/// <summary>
/// A window of messages within a thread, plus how much of the thread lies outside it in each
/// direction so the client can offer to page either way.
///
/// <see cref="MessagesBefore"/> and <see cref="MessagesAfter"/> always describe the slice in
/// <see cref="Messages"/>, not whatever the caller already holds. A caller extending an existing
/// window therefore takes only the count for the end it extended: prepending an older page updates
/// its "before" count and leaves its "after" count alone, and vice versa.
/// </summary>
public class MessageThreadPageModel
{
    /// <summary>
    /// The messages in this window, oldest first.
    /// </summary>
    public List<MessageModel> Messages { get; set; } = [];

    /// <summary>
    /// How many messages in the thread are older than the first message in this window.
    /// </summary>
    public int MessagesBefore { get; set; }

    /// <summary>
    /// How many messages in the thread are newer than the last message in this window.
    /// </summary>
    public int MessagesAfter { get; set; }

    /// <summary>
    /// The first message the caller hasn't read, when the window was anchored on it. Null when the
    /// caller is up to date, has never opened the thread before (in which case there is no sensible
    /// "since when", so the newest messages win rather than dumping a first-time reader at the top
    /// of a years-old thread), or when this is a follow-up page rather than the initial load.
    ///
    /// The client marks the boundary with a "new messages" separator and opens the thread there.
    /// </summary>
    public Guid? FirstUnreadMessageID { get; set; }
}
