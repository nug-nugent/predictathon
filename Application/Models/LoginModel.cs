namespace Predictathon.Application.Models;

/// <summary>
/// Model used when logging in.
/// </summary>
public class LoginModel
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
