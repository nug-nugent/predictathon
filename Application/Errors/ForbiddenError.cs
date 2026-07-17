using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// Signals that the caller is authenticated but not permitted to perform the requested operation
/// (e.g. Identity.Users.CanViewMessageboard is false), allowing callers to distinguish this from
/// <see cref="UnauthorizedError"/> (not authenticated at all) and map it to an HTTP 403 by type.
/// </summary>
public class ForbiddenError : Error
{
    /// <summary>
    /// Creates a new <see cref="ForbiddenError"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public ForbiddenError(string message = "Forbidden") : base(message)
    {
    }
}
