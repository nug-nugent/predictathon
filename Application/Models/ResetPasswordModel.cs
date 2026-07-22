namespace Predictathon.Application.Models;

/// <summary>
/// Model used to complete a password reset with the token emailed by ForgotPassword.
/// </summary>
public class ResetPasswordModel
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
