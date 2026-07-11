namespace Predictathon.Application.Models;

/// <summary>
/// Model used when registering a new user.
/// </summary>
public class RegisterModel
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// See <see cref="LoginModel.RememberMe"/>.
    /// </summary>
    public bool RememberMe { get; set; }
}
