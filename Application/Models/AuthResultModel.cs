namespace Predictathon.Application.Models;

/// <summary>
/// The result of a successful register/login: a bearer token and its expiry.
/// </summary>
public class AuthResultModel
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
