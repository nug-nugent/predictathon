using FluentResults;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Predictathon.Application.Services;

[ScopedService]
public class MessageImageService : IMessageImageService
{
    // Matches the legacy MaxWidth/MaxHeight bounds (fit-within, not cropped).
    private const int LargeMaxDimension = 1200;
    private const int SmallMaxDimension = 400;

    // Defends against decompression-bomb-style uploads/downloads.
    private const int MaxSourceDimension = 6000;

    private static readonly JpegEncoder Encoder = new() { Quality = 90 };
    private static readonly HttpClient HttpClient = new();

    private readonly IConfiguration _configuration;

    public MessageImageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<Result> SaveFromStreamAsync(Guid messageId, Stream imageStream, CancellationToken cancellationToken = default)
        => await SaveAsync(messageId, imageStream, cancellationToken);

    /// <inheritdoc />
    public async Task<Result> SaveFromUrlAsync(Guid messageId, string imageUrl, CancellationToken cancellationToken = default)
    {
        Stream downloadStream;
        try
        {
            downloadStream = await HttpClient.GetStreamAsync(imageUrl, cancellationToken);
        }
        catch (Exception)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "Couldn't download an image from that URL."));
        }

        await using (downloadStream)
        {
            return await SaveAsync(messageId, downloadStream, cancellationToken);
        }
    }

    /// <inheritdoc />
    public string? GetImageUrl(Guid messageId, bool hasLinkedImage)
    {
        if (!hasLinkedImage)
        {
            return null;
        }

        var baseUrl = _configuration["MessageImages:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            return null;
        }

        return $"{baseUrl}/uploads/message-images/{messageId}_sm.jpg";
    }

    private async Task<Result> SaveAsync(Guid messageId, Stream imageStream, CancellationToken cancellationToken)
    {
        Image image;
        try
        {
            image = await Image.LoadAsync(imageStream, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "The image isn't a recognised image format."));
        }
        catch (InvalidImageContentException)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "The image isn't a recognised image format."));
        }

        using (image)
        {
            if (image.Width > MaxSourceDimension || image.Height > MaxSourceDimension)
            {
                return Result.Fail(new PropertyValidationError(string.Empty, $"Image dimensions must not exceed {MaxSourceDimension}x{MaxSourceDimension}."));
            }

            var directory = GetStorageDirectory();
            Directory.CreateDirectory(directory);

            using (var large = image.Clone(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(LargeMaxDimension, LargeMaxDimension) })))
            {
                await large.SaveAsJpegAsync(GetFilePath(messageId, small: false), Encoder, cancellationToken);
            }

            using (var small = image.Clone(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(SmallMaxDimension, SmallMaxDimension) })))
            {
                await small.SaveAsJpegAsync(GetFilePath(messageId, small: true), Encoder, cancellationToken);
            }
        }

        return Result.Ok();
    }

    private string GetStorageDirectory()
        => Path.GetFullPath(_configuration["MessageImages:StoragePath"] ?? "Uploads/MessageImages");

    private string GetFilePath(Guid messageId, bool small)
        => Path.Combine(GetStorageDirectory(), small ? $"{messageId}_sm.jpg" : $"{messageId}.jpg");
}
