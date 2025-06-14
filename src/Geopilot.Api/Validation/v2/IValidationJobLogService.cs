using Geopilot.Api.Models;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Service for retrieving validation log files from various storage backends.
/// </summary>
public interface IValidationJobLogService
{
    /// <summary>
    /// Retrieves the content of a validation log as a stream.
    /// </summary>
    /// <param name="logId">The unique identifier of the validation log.</param>
    /// <returns>
    /// A stream containing the log content, or null if the log is not found.
    /// The caller is responsible for disposing the stream.
    /// </returns>
    Task<Stream?> GetLogContentAsync(Guid logId);

    /// <summary>
    /// Retrieves metadata about a validation log without downloading the content.
    /// </summary>
    /// <param name="logId">The unique identifier of the validation log.</param>
    /// <returns>
    /// The ValidationJobLog entity with navigation properties, or null if not found.
    /// </returns>
    Task<ValidationJobLog?> GetLogInfoAsync(Guid logId);
}
