namespace Predictathon.Application.Options;

/// <summary>
/// PayPal API settings, bound from the "PayPal" configuration section. Lives in Application (rather
/// than alongside WebApi/Options, like most other options types) because it's consumed by an
/// Application-layer service, which can't reference the WebApi project.
/// </summary>
public sealed class PayPalOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "PayPal";

    /// <summary>Either "sandbox" or "live", selecting which PayPal API base URL to call.</summary>
    public string Mode { get; init; } = "sandbox";

    /// <summary>The OAuth2 client id for PayPal's client-credentials grant.</summary>
    public string ClientId { get; init; } = "";

    /// <summary>The OAuth2 client secret for PayPal's client-credentials grant.</summary>
    public string ClientSecret { get; init; } = "";
}
