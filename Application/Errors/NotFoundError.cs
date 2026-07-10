using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// Signals that a requested entity could not be found, allowing callers to
/// distinguish "not found" failures from other kinds of <see cref="Result"/> errors by type.
/// </summary>
public class NotFoundError : Error
{
    /// <summary>
    /// Creates a new <see cref="NotFoundError"/>.
    /// </summary>
    /// <param name="message">A human-readable description of what was not found.</param>
    public NotFoundError(string message = "Entity not found") : base(message)
    {
    }
}
