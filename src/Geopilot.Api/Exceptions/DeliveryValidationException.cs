namespace Geopilot.Api.Exceptions;

/// <summary>
/// Exception thrown when a delivery request fails business rule validation (e.g., mandate rules).
/// This typically results in a 400 Bad Request HTTP response.
/// </summary>
public class DeliveryValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryValidationException"/> class.
    /// </summary>
    public DeliveryValidationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DeliveryValidationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryValidationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public DeliveryValidationException(string message, Exception inner)
        : base(message, inner) { }
}
