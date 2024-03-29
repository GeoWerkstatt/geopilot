﻿using Geopilot.Api.FileAccess;

namespace Geopilot.Api.Validation;

/// <summary>
/// Provides methods to start validation jobs and access job status information.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Creates a new <see cref="ValidationJob"/>.
    /// </summary>
    /// <param name="originalFileName">Name of the uploaded file.</param>
    /// <returns>The created <see cref="ValidationJob"/> and a <see cref="FileHandle"/> to store the file to validate.</returns>
    (ValidationJob ValidationJob, FileHandle FileHandle) CreateValidationJob(string originalFileName);

    /// <summary>
    /// Starts the validation job asynchronously.
    /// </summary>
    /// <param name="validationJob">The validation job to start.</param>
    /// <returns>Current job status information.</returns>
    Task<ValidationJobStatus> StartValidationJobAsync(ValidationJob validationJob);

    /// <summary>
    /// Gets the validation job.
    /// </summary>
    /// <param name="jobId">The id of the validation job.</param>
    /// <returns>Validation job with the specified <paramref name="jobId"/>.</returns>
    ValidationJob? GetJob(Guid jobId);

    /// <summary>
    /// Gets the validation job status.
    /// </summary>
    /// <param name="jobId">The id of the validation job.</param>
    /// <returns>Status information for the validation job with the specified <paramref name="jobId"/>.</returns>
    ValidationJobStatus? GetJobStatus(Guid jobId);

    /// <summary>
    /// Gets all file extensions that are supported for upload.
    /// All entries start with a "." like ".txt", ".xml" and the collection can include ".*" (all files allowed).
    /// </summary>
    /// <returns>Supported file extensions.</returns>
    Task<ICollection<string>> GetSupportedFileExtensionsAsync();

    /// <summary>
    /// Checks if the specified <paramref name="fileExtension"/> is supported for upload.
    /// </summary>
    /// <param name="fileExtension">Extension of the uploaded file starting with ".".</param>
    /// <returns>True, if the <paramref name="fileExtension"/> is supported.</returns>
    Task<bool> IsFileExtensionSupportedAsync(string fileExtension);
}
