using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Services;

namespace Predictathon.UnitTests.Services;

public class AvatarServiceClampTests
{
    [Fact]
    public void ClampToImageBounds_CropWithinBounds_IsUnchanged()
    {
        var crop = new AvatarCropRect { X = 10, Y = 20, Width = 100, Height = 80 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.X.Should().Be(10);
        result.Y.Should().Be(20);
        result.Width.Should().Be(100);
        result.Height.Should().Be(80);
    }

    [Fact]
    public void ClampToImageBounds_NegativeOrigin_ClampsToZero()
    {
        var crop = new AvatarCropRect { X = -50, Y = -30, Width = 100, Height = 80 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.X.Should().Be(0);
        result.Y.Should().Be(0);
    }

    [Fact]
    public void ClampToImageBounds_WidthExceedsImage_IsClampedToRemainingSpace()
    {
        var crop = new AvatarCropRect { X = 450, Y = 0, Width = 200, Height = 50 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.X.Should().Be(450);
        result.Width.Should().Be(50);
    }

    [Fact]
    public void ClampToImageBounds_HeightExceedsImage_IsClampedToRemainingSpace()
    {
        var crop = new AvatarCropRect { X = 0, Y = 380, Width = 50, Height = 200 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.Y.Should().Be(380);
        result.Height.Should().Be(20);
    }

    [Fact]
    public void ClampToImageBounds_OriginBeyondImage_ProducesZeroSizeRectangle()
    {
        var crop = new AvatarCropRect { X = 600, Y = 500, Width = 100, Height = 100 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.X.Should().Be(500);
        result.Y.Should().Be(400);
        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    [Fact]
    public void ClampToImageBounds_NegativeWidthAndHeight_ClampToZero()
    {
        var crop = new AvatarCropRect { X = 10, Y = 10, Width = -20, Height = -20 };

        var result = AvatarService.ClampToImageBounds(crop, imageWidth: 500, imageHeight: 400);

        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }
}
