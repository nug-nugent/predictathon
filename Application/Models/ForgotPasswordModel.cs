namespace Predictathon.Application.Models;

/// <summary>
/// Model used to request a password-reset email.
/// </summary>
public class ForgotPasswordModel
{
    public string UserNameOrEmail { get; set; } = string.Empty;
}
