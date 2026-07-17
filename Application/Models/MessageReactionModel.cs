namespace Predictathon.Application.Models;

/// <summary>
/// A single reaction on a message. The reaction name/image are free-form values supplied by the
/// client from a fixed catalog it owns - the server just stores and returns what it's given.
/// </summary>
public class MessageReactionModel
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public string ReactionName { get; set; } = "";

    public string ImageUrl { get; set; } = "";
}
