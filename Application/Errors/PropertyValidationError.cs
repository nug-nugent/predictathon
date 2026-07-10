using FluentResults;

namespace Predictathon.Application.Errors;

/// <summary>
/// A validation failure associated with a specific model property, letting callers
/// group errors by property without parsing "Property: message" text out of the error message.
/// </summary>
public class PropertyValidationError : Error
{
    /// <summary>
    /// The name of the property the validation failure relates to.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Creates a new <see cref="PropertyValidationError"/>.
    /// </summary>
    /// <param name="propertyName">The name of the invalid property.</param>
    /// <param name="message">The validation failure message.</param>
    public PropertyValidationError(string propertyName, string message) : base(message)
    {
        PropertyName = propertyName;
    }
}
