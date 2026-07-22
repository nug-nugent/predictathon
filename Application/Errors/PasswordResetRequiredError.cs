using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// Signals that a login attempt matched a real account but that account has no usable password
/// set yet (e.g. a legacy dbo.User account migrated into Identity.Users with PasswordHash left
/// null) - allowing callers to distinguish this from a plain wrong-password failure by type, and
/// show a "reset your password" flow instead of a generic "invalid username or password" message.
/// </summary>
public class PasswordResetRequiredError : Error
{
    /// <summary>
    /// Creates a new <see cref="PasswordResetRequiredError"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public PasswordResetRequiredError(string message = "This account needs a password reset before you can log in.") : base(message)
    {
    }
}
