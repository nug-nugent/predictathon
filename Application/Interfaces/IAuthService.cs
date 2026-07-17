using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Creates a new user and returns a bearer token plus a raw refresh token for them.
    /// </summary>
    Task<Result<AuthTokenResult>> Register(RegisterModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies credentials and returns a bearer token plus a raw refresh token on success.
    /// </summary>
    Task<Result<AuthTokenResult>> Login(LoginModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid, active refresh token for a fresh bearer token. Does not rotate the
    /// refresh token - the same one remains valid until its own expiry or explicit revocation.
    /// </summary>
    Task<Result<AuthResultModel>> RefreshToken(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the given refresh token, if any. Always succeeds - logging out an already-invalid
    /// or missing token is not an error.
    /// </summary>
    Task Logout(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emails a password-reset link if the given username/email matches an account. Always
    /// succeeds - deliberately doesn't reveal whether the account exists.
    /// </summary>
    Task<Result> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a password reset using the token emailed by <see cref="ForgotPassword"/>.
    /// </summary>
    Task<Result> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken = default);
}
