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
    /// The public URL for a user's avatar image, or null if they haven't uploaded one (or the file
    /// has gone missing). The URL carries a version stamp taken from the file itself, so replacing
    /// an avatar changes its URL and browsers can't serve the previous picture from cache.
    /// </summary>
    /// <param name="userId">The user whose avatar is wanted.</param>
    /// <param name="imageUploaded">The user's Identity.Users.ImageUploaded flag.</param>
    /// <param name="large">
    /// True for the full-size image (shown when a picture is opened on the profile page), false for
    /// the thumbnail used beside a name. Falls back to the thumbnail when no large file exists.
    /// </param>
    string? GetAvatarUrl(Guid userId, bool imageUploaded, bool large = false);
}
