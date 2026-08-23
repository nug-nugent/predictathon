using System.Globalization;
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

    // Per-request memo of avatar file versions - see GetFileVersion. Cleared whenever this instance
    // writes or deletes a file, so a URL handed back after an upload can't be a stale entry.
    private readonly Dictionary<string, string?> _fileVersions = [];

    public AvatarService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    /// <inheritdoc />
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

        _fileVersions.Clear();

        await SetImageUploadedAsync(userId, true, cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DeleteIfExists(GetFilePath(userId, small: false));
        DeleteIfExists(GetFilePath(userId, small: true));
        _fileVersions.Clear();

        await SetImageUploadedAsync(userId, false, cancellationToken);
    }

    /// <inheritdoc />
    public string? GetAvatarUrl(Guid userId, bool imageUploaded, bool large = false)
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

        var version = GetFileVersion(userId, small: !large);
        if (version is null)
        {
            // Avatars uploaded through the legacy app didn't always leave a large file behind, so
            // fall back to the thumbnail rather than pointing at a file that isn't there. A missing
            // thumbnail means there's nothing to show at all - the initials fallback handles it.
            return large ? GetAvatarUrl(userId, imageUploaded, large: false) : null;
        }

        var fileName = large ? $"{userId}.jpg" : $"{userId}_sm.jpg";

        // The filename is fixed per user, so a re-upload would otherwise keep being served from the
        // browser's cache (and from any cache in between) under the URL it already holds. Stamping
        // the file's own last-write time onto the URL changes it whenever the picture changes,
        // which is what lets the images be cached hard in the first place - see Program.cs.
        return $"{baseUrl}/uploads/avatars/{fileName}?v={version}";
    }

    /// <summary>
    /// A short version token for one of a user's avatar files - its last-write time - or null if
    /// the file doesn't exist. Memoised for the lifetime of this (scoped, so per-request) instance,
    /// since a league table or message board page resolves URLs for many users at once and often
    /// the same user repeatedly.
    /// </summary>
    /// <param name="userId">The user whose avatar file is being stamped.</param>
    /// <param name="small">True for the thumbnail file, false for the large one.</param>
    private string? GetFileVersion(Guid userId, bool small)
    {
        var path = GetFilePath(userId, small);
        if (_fileVersions.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var file = new FileInfo(path);
        var version = file.Exists ? file.LastWriteTimeUtc.Ticks.ToString("x", CultureInfo.InvariantCulture) : null;
        _fileVersions[path] = version;

        return version;
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

    /// <summary>
    /// Clamps a client-supplied crop rectangle to the bounds of the source image, so an out-of-range
    /// or negative crop can't be passed to ImageSharp's Crop.
    /// </summary>
    /// <param name="crop">The requested crop rectangle, in the source image's pixel coordinates.</param>
    /// <param name="imageWidth">The source image's width, in pixels.</param>
    /// <param name="imageHeight">The source image's height, in pixels.</param>
    internal static Rectangle ClampToImageBounds(AvatarCropRect crop, int imageWidth, int imageHeight)
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
