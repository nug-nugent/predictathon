using FluentResults;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Stores the images linked to messageboard posts (either uploaded directly or downloaded from a
/// pasted URL), mirroring the legacy behaviour of always re-hosting the image locally as a jpg.
/// </summary>
public interface IMessageImageService
{
    /// <summary>
    /// Decodes, resizes, and saves an uploaded image as the given message's linked image.
    /// </summary>
    Task<Result> SaveFromStreamAsync(Guid messageId, Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the image at the given URL and saves it the same way as <see cref="SaveFromStreamAsync"/>.
    /// </summary>
    Task<Result> SaveFromUrlAsync(Guid messageId, string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// The public URL for a message's linked image, or null if it has none.
    /// </summary>
    string? GetImageUrl(Guid messageId, bool hasLinkedImage);
}
