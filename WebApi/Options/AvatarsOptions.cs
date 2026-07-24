namespace Predictathon.WebApi.Options;

/// <summary>
/// Avatar file storage settings, bound from the "Avatars" configuration section.
/// </summary>
public sealed class AvatarsOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "Avatars";

    /// <summary>Filesystem path (relative or absolute) where uploaded avatars are stored.</summary>
    public string StoragePath { get; init; } = "Uploads/Avatars";
}
