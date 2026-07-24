namespace Predictathon.WebApi.Options;

/// <summary>
/// Message board image storage settings, bound from the "MessageImages" configuration section.
/// </summary>
public sealed class MessageImagesOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "MessageImages";

    /// <summary>Filesystem path (relative or absolute) where uploaded message images are stored.</summary>
    public string StoragePath { get; init; } = "Uploads/MessageImages";
}
