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

    public List<MessageReactionModel> Reactions { get; set; } = [];
}
