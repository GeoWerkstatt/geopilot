namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Defines a service for processing virus scan operations on validation job files.
/// </summary>
public interface IVirusScanService
{
    /// <summary>
    /// Processes virus scan status updates for all files within a validation job.
    /// </summary>
    /// <param name="validationJobId">
    /// The unique identifier of the validation job whose files should be processed for virus scan status updates.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation. The task completes when all files
    /// in the job have had their virus scan status checked and updated accordingly.
    /// </returns>
    Task ProcessJobForVirusScansAsync(Guid validationJobId);
}
