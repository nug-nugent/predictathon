namespace Predictathon.Application.Models;

/// <summary>
/// A crop rectangle in the ORIGINAL uploaded image's pixel coordinates, as chosen by the user in
/// the client-side cropper. Only a hint - the server clamps it to the image's actual bounds before
/// applying it, so an out-of-range value can't crash or corrupt processing.
/// </summary>
public class AvatarCropRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}
