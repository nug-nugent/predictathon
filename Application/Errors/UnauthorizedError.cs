using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// Signals that a request could not be authenticated (e.g. a missing or invalid refresh token),
/// allowing callers to distinguish this from other kinds of <see cref="Result"/> errors by type.
/// </summary>
public class UnauthorizedError : Error
{
    /// <summary>
    /// Creates a new <see cref="UnauthorizedError"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public UnauthorizedError(string message = "Unauthorized") : base(message)
    {
    }
}
