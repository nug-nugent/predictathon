namespace Predictathon.Application.Models;

/// <summary>
/// Internal result of a successful register/login/refresh: the response to actually send the
/// client (<see cref="Response"/>) plus the raw refresh token. The raw token must never be
/// serialized into a JSON response body - it belongs only in a Set-Cookie header, otherwise
/// client-side JS could read it, which defeats the point of an HttpOnly cookie. Controllers
/// should unwrap this, set the cookie from <see cref="RefreshToken"/>, and return only
/// <see cref="Response"/> to the caller.
/// </summary>
public class AuthTokenResult
{
    public AuthResultModel Response { get; set; } = new();

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
