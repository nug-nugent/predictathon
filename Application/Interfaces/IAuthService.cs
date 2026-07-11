using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Creates a new user and returns a bearer token for them.
    /// </summary>
    Task<Result<AuthResultModel>> Register(RegisterModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies credentials and returns a bearer token on success.
    /// </summary>
    Task<Result<AuthResultModel>> Login(LoginModel model, CancellationToken cancellationToken = default);
}
