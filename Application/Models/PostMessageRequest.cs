namespace Predictathon.Application.Models;

/// <summary>
/// Posts a message with plain text and/or a YouTube link and/or an externally-hosted image URL
/// (which the server downloads and re-hosts). For an uploaded image file, see the multipart
/// WebApi/Models/PostMessageImageRequest.cs variant instead.
/// </summary>
public class PostMessageRequest
{
    public string? Content { get; set; }

    public string? YouTubeUrl { get; set; }

    public string? ImageUrl { get; set; }
}
