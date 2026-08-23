using FluentResults;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.IntegrationTests.TestDoubles;

/// <summary>
/// Stand-in for <see cref="IAvatarService"/> that resolves avatar URLs without any Identity or
/// configuration plumbing, so tests can assert that a league table row's URL follows the
/// ImageUploaded flag the stored procedure returned. Upload/remove aren't exercised here.
/// </summary>
public class StubAvatarService : IAvatarService
{
    /// <summary>
    /// The URL returned for any user who has uploaded an avatar.
    /// </summary>
    public const string AvatarUrl = "https://example.test/uploads/avatars/stub_sm.jpg";

    /// <inheritdoc />
    public Task<Result> UploadAvatarAsync(Guid userId, Stream imageStream, AvatarCropRect crop, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public string? GetAvatarUrl(Guid userId, bool imageUploaded) => imageUploaded ? AvatarUrl : null;
}
