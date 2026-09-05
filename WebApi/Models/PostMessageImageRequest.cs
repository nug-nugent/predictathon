namespace Predictathon.WebApi.Models;

/// <summary>
/// Multipart form body for posting a message with an uploaded image. Kept in WebApi rather than
/// Application since IFormFile is an ASP.NET Core hosting type - Application stays host-agnostic.
/// </summary>
public class PostMessageImageRequest
{
    public IFormFile Image { get; set; } = null!;

    public string? Content { get; set; }

    /// <summary>
    /// The message being replied to, or null for an ordinary post. Must be in the same thread.
    /// </summary>
    public Guid? ReplyToMessageID { get; set; }
}
