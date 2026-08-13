namespace Predictathon.WebApi.Options;

/// <summary>
/// Data Protection key storage settings, bound from the "DataProtection" configuration section.
/// </summary>
public sealed class DataProtectionKeysOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "DataProtection";

    /// <summary>Filesystem path (relative or absolute) where Data Protection keys are persisted.</summary>
    public string KeysPath { get; init; } = "Keys";
}
