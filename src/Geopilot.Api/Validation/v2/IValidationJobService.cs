using Geopilot.Api.Contracts;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Defines the service contract for managing validation job lifecycle.
/// This service focuses solely on job creation, startup, and status reporting.
/// </summary>
public interface IValidationJobService
{
    /// <summary>
    /// Creates a new empty validation job ready for file attachment.
    /// </summary>
    /// <returns>The created validation job with generated ID and initial status.</returns>
    Task<Models.ValidationJob> CreateValidationJobAsync();

    /// <summary>
    /// Initiates the validation process for a job after all files have been uploaded.
    /// This method transitions the job from preparation phase to active validation processing.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validation job to start processing.</param>
    /// <param name="request">The validation request containing processing parameters, such as the mandate ID.</param>
    /// <returns>
    /// A <see cref="ValidationResponse"/> containing the initial processing status and job information,
    /// or null if the job was not found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the provided mandate ID is invalid or not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the job cannot be started (e.g., wrong status, no files).</exception>
    Task<ValidationResponse?> StartValidationAsync(Guid jobId, ValidationRequest request);

    /// <summary>
    /// Retrieves the current status and progress information for a validation job.
    /// This method provides real-time status updates for monitoring job progress and results.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validation job to query.</param>
    /// <returns>
    /// A <see cref="ValidationStatusResponse"/> containing current job status, file processing details,
    /// and any error information, or null if the job was not found.
    /// </returns>
    Task<ValidationStatusResponse?> GetJobStatusAsync(Guid jobId);
}
