namespace Predictathon.WebApi.Models;

/// <summary>
/// Multipart form body for uploading an avatar: the original image plus the crop rectangle chosen
/// client-side (in the original image's pixel coordinates). Kept in WebApi rather than Application
/// since IFormFile is an ASP.NET Core hosting type - Application stays host-agnostic.
/// </summary>
public class UploadAvatarRequest
{
    public IFormFile Image { get; set; } = null!;

    public int CropX { get; set; }

    public int CropY { get; set; }

    public int CropWidth { get; set; }

    public int CropHeight { get; set; }
}
