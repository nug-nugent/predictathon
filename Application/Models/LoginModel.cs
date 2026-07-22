namespace Predictathon.Application.Models;

/// <summary>
/// Model used when logging in.
/// </summary>
public class LoginModel
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether the issued refresh token should survive a browser restart (a persistent cookie
    /// with a long expiry) or only the current browser session (a session cookie with a short
    /// server-side expiry as a backstop).
    /// </summary>
    public bool RememberMe { get; set; }
}
