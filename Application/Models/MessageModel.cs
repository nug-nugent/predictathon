namespace Predictathon.Application.Models;

/// <summary>
/// A single post within a message thread.
/// </summary>
public class MessageModel
{
    public Guid MessageID { get; set; }

    public Guid MessageThreadID { get; set; }

    public Guid PostedByUserID { get; set; }

    public string PostedByUsername { get; set; } = "";

    public string? PostedByAvatarUrl { get; set; }

    public DateTime MessageDateTime { get; set; }

    public string? MessageContent { get; set; }

    public string? YouTubeVideoID { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// The poster's total messageboard post count at the time this message was posted.
    /// </summary>
    public int PosterTotalMessageboardPosts { get; set; }

    /// <summary>
    /// The poster's trophies, so their wins show beside their name without the board having to
    /// fetch a profile per author.
    /// </summary>
    public List<UserTrophyModel> PosterTrophies { get; set; } = [];

    /// <summary>
    /// The message this one replies to, or null for an ordinary post. Always a message in the same
    /// thread.
    /// </summary>
    public MessageReplyReferenceModel? ReplyTo { get; set; }

    public List<MessageReactionModel> Reactions { get; set; } = [];
}
