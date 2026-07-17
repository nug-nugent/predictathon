using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Domain.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Predictathon.Application.Services;

[ScopedService]
public class AvatarService : IAvatarService
{
    // Matches the legacy 10:8 crop aspect ratio.
    private const int LargeWidth = 400;
    private const int LargeHeight = 320;
    private const int SmallWidth = 160;
    private const int SmallHeight = 128;

    // Defends against decompression-bomb-style uploads (a small file that decodes to an enormous
    // bitmap) - rejected before any resize work happens.
    private const int MaxSourceDimension = 6000;

    private static readonly JpegEncoder Encoder = new() { Quality = 90 };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AvatarService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<Result> UploadAvatarAsync(Guid userId, Stream imageStream, AvatarCropRect crop, CancellationToken cancellationToken = default)
    {
        Image image;
        try
        {
            image = await Image.LoadAsync(imageStream, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "The uploaded file isn't a recognised image format."));
        }
        catch (InvalidImageContentException)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "The uploaded file isn't a recognised image format."));
        }

        using (image)
        {
            if (image.Width > MaxSourceDimension || image.Height > MaxSourceDimension)
            {
                return Result.Fail(new PropertyValidationError(string.Empty, $"Image dimensions must not exceed {MaxSourceDimension}x{MaxSourceDimension}."));
            }

            var rect = ClampToImageBounds(crop, image.Width, image.Height);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return Result.Fail(new PropertyValidationError(string.Empty, "Invalid crop area."));
            }

            image.Mutate(x => x.Crop(rect));

            var directory = GetStorageDirectory();
            Directory.CreateDirectory(directory);

            using (var large = image.Clone(x => x.Resize(LargeWidth, LargeHeight)))
            {
                await large.SaveAsJpegAsync(GetFilePath(userId, small: false), Encoder, cancellationToken);
            }

            using (var small = image.Clone(x => x.Resize(SmallWidth, SmallHeight)))
            {
                await small.SaveAsJpegAsync(GetFilePath(userId, small: true), Encoder, cancellationToken);
            }
        }

        await SetImageUploadedAsync(userId, true, cancellationToken);

        return Result.Ok();
    }

    public async Task RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DeleteIfExists(GetFilePath(userId, small: false));
        DeleteIfExists(GetFilePath(userId, small: true));

        await SetImageUploadedAsync(userId, false, cancellationToken);
    }

    public string? GetAvatarUrl(Guid userId, bool imageUploaded)
    {
        if (!imageUploaded)
        {
            return null;
        }

        var baseUrl = _configuration["Avatars:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            return null;
        }

        return $"{baseUrl}/uploads/avatars/{userId}_sm.jpg";
    }

    private async Task SetImageUploadedAsync(Guid userId, bool imageUploaded, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        user.ImageUploaded = imageUploaded;
        await _userManager.UpdateAsync(user);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static Rectangle ClampToImageBounds(AvatarCropRect crop, int imageWidth, int imageHeight)
    {
        var x = Math.Clamp(crop.X, 0, imageWidth);
        var y = Math.Clamp(crop.Y, 0, imageHeight);
        var width = Math.Clamp(crop.Width, 0, imageWidth - x);
        var height = Math.Clamp(crop.Height, 0, imageHeight - y);

        return new Rectangle(x, y, width, height);
    }

    private string GetStorageDirectory()
        => Path.GetFullPath(_configuration["Avatars:StoragePath"] ?? "Uploads/Avatars");

    private string GetFilePath(Guid userId, bool small)
        => Path.Combine(GetStorageDirectory(), small ? $"{userId}_sm.jpg" : $"{userId}.jpg");
}
