using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IAvatarService
{
    /// <summary>
    /// Decodes, crops, and resizes an uploaded image into the user's large and small avatar
    /// files, re-encoding it server-side rather than trusting client-produced output. Marks
    /// Identity.Users.ImageUploaded once saved.
    /// </summary>
    Task<Result> UploadAvatarAsync(Guid userId, Stream imageStream, AvatarCropRect crop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user's avatar files (if any) and clears Identity.Users.ImageUploaded.
    /// </summary>
    Task RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The public URL for a user's small avatar image, or null if they haven't uploaded one.
    /// </summary>
    string? GetAvatarUrl(Guid userId, bool imageUploaded);
}
