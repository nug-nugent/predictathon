using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// Signals that an operation could not be completed because it conflicts with the current state
/// of the resource (e.g. a duplicate key), allowing callers to map this to an HTTP 409 by type
/// rather than by inspecting error message text.
/// </summary>
public class ConflictError : Error
{
    /// <summary>
    /// Creates a new <see cref="ConflictError"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the conflict.</param>
    public ConflictError(string message = "The request could not be completed due to a conflict with the current state of the resource.") : base(message)
    {
    }
}
