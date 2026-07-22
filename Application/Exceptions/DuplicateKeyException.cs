namespace Predictathon.Application.Exceptions;

/// <summary>
/// Thrown by a persistence implementation when a write would violate a primary key or unique
/// constraint, so callers don't need to depend on provider-specific exception types (e.g. SqlException).
/// </summary>
public class DuplicateKeyException : Exception
{
    /// <summary>
    /// Creates a new <see cref="DuplicateKeyException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the conflict.</param>
    /// <param name="innerException">The underlying provider exception, if any.</param>
    public DuplicateKeyException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
