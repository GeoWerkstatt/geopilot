using Geopilot.Api.Models;

namespace Geopilot.Api.Contracts;

/// <summary>
/// Represents a reference to a specific log file generated during the file validation process.
/// It provides a unique identifier for downloading the log and a descriptive name.
/// </summary>
public sealed class ValidationLog
{
    /// <summary>
    /// Gets or sets the unique identifier of the validation log.
    /// </summary>
    /// <value>
    /// The primary key used to retrieve the full content of this specific log file.
    /// </value>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the descriptive name of the log.
    /// </summary>
    /// <value>
    /// A human-readable identifier for the log, indicating its type or the validation tool that generated it.
    /// </value>
    /// <example>ili-validator.log</example>
    public string LogName { get; set; } = string.Empty;
}

/// <summary>
/// Represents the validation status and results for a single file within a validation job.
/// </summary>
public sealed class FileReport
{
    /// <summary>
    /// Gets or sets the original filename as uploaded by the user.
    /// </summary>
    /// <value>
    /// The filename including extension, preserved exactly as provided during upload.
    /// Used for display purposes and user reference.
    /// </value>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current processing status of this file.
    /// </summary>
    /// <value>
    /// The current stage in the file's validation lifecycle. Status progression
    /// indicates whether the file is queued, being processed, or completed.
    /// </value>
    public FileStatus FileStatus { get; set; }

    /// <summary>
    /// Gets or sets the generic validation results for this file.
    /// </summary>
    /// <value>
    /// A structured object containing validation outcomes, error details, and processing metadata.
    /// The exact structure varies by validator type. <c>null</c> if validation has not yet been performed.
    /// </value>
    public string? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this file was successfully uploaded to storage.
    /// </summary>
    /// <value>
    /// The UTC date and time when file upload completed, or <c>null</c> if the upload
    /// timestamp is unavailable or the file upload failed.
    /// </value>
    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the size of the uploaded file in bytes.
    /// </summary>
    /// <value>
    /// The exact file size in bytes, or <c>null</c> if size information is unavailable.
    /// Used for progress tracking, storage management, and user interface display.
    /// </value>
    /// <example>
    /// A 1.5 MB file would have <c>FileSizeBytes = 1572864</c>
    /// </example>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets a list of log files associated with this file's validation process.
    /// </summary>
    /// <value>A list of <see cref="ValidationLog"/> references, or <c>null</c> if no logs are available.</value>
    public IList<ValidationLog>? Logs { get; set; }
}

/// <summary>
/// Represents the complete status of a validation job including overall progress
/// and individual file results.
/// </summary>
public sealed class ValidationStatusResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the validation job being tracked.
    /// </summary>
    /// <value>
    /// The GUID that uniquely identifies this validation job across the system.
    /// This ID is provided when the job is initially created and remains constant throughout its lifecycle.
    /// </value>
    public Guid ValidationJobId { get; set; }

    /// <summary>
    /// Gets or sets the overall status of the validation job.
    /// </summary>
    /// <value>
    /// The current stage of the entire validation job, aggregating the status of all constituent files.
    /// </value>
    public ValidationJobStatus ValidationJobStatus { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the mandate governing this validation job's requirements.
    /// </summary>
    /// <value>
    /// The mandate ID that determines validation rules and compliance standards for this job,
    /// or <c>null</c> if default validation behavior is being used.
    /// </value>
    public int? MandateId { get; set; }

    /// <summary>
    /// Gets or sets the reason why the validation job failed.
    /// </summary>
    /// <value>
    /// A human-readable description of the failure cause, or <c>null</c> if the job
    /// completed successfully or is still in progress.
    /// </value>
    public string? Errors { get; set; }

    /// <summary>
    /// Gets or sets the collection of individual file validation reports.
    /// </summary>
    /// <value>
    /// A read-only list containing one <see cref="FileReport"/> for each file
    /// included in this validation job. Always contains at least one element
    /// </value>
    public IReadOnlyList<FileReport> FileReports { get; set; } = Array.Empty<FileReport>();
}
