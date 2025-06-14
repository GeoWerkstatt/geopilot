namespace Geopilot.Api.Exceptions;

/// <summary>
/// Exception thrown when a validation job cannot be found or is not in a processable state.
/// This typically results in a 404 Not Found HTTP response.
/// </summary>
public class ValidationJobNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationJobNotFoundException"/> class.
    /// </summary>
    public ValidationJobNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationJobNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ValidationJobNotFoundException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationJobNotFoundException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ValidationJobNotFoundException(string message, Exception inner)
        : base(message, inner) { }
}
