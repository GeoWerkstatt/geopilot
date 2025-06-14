namespace Geopilot.Api.Models;

/// <summary>
/// Meta information on how an asset was created and how it has to be interpreted.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Primary data delivered by the user.
    /// </summary>
    PrimaryData,

    /// <summary>
    /// Reports created by the validation process.
    /// </summary>
    ValidationReport,

    /// <summary>
    /// Metadata created by the declaration or validation process.
    /// </summary>
    Metadata,
}

/// <summary>
/// Defines how <see cref="Delivery"/> fileds have to be evaluated.
/// </summary>
public enum FieldEvaluationType
{
    /// <summary>
    /// Field must not contain any value.
    /// </summary>
    NotEvaluated,

    /// <summary>
    /// Field may contain a value but must not.
    /// </summary>
    Optional,

    /// <summary>
    /// Field must contan a value.
    /// </summary>
    Required,
}

// Located in Geopilot.Api.Models (as you have it)

/// <summary>
/// Describes the high-level life-cycle of a V2 validation job.
/// This represents the state of the *entire collection* of files.
/// </summary>
public enum ValidationJobStatus
{
    /// <summary>
    /// Job record created. Client is expected to upload files using provided SAS URLs.
    /// Client can still add more files to the job.
    /// </summary>
    Pending, // Initial state after /upload-request or /add-files

    // Removed 'Uploading' as it's a client-side activity.
    // The backend considers the job 'Pending' until '/start' is called.

    /// <summary>
    /// Client has called /start. All file definitions are complete.
    /// The job is now in the queue for background processing, starting with virus scans.
    /// No more files can be added.
    /// </summary>
    Queued, // After /start endpoint is successfully called

    /// <summary>
    /// At least one file in the job is actively being scanned for viruses by Microsoft Defender,
    /// OR the system is polling Defender for scan results for one or more files.
    /// This state persists until all files are either Clean or Infected.
    /// </summary>
    AwaitingVirusScanResults, // VirusScanMonitor is actively working on this job

    /// <summary>
    /// All files in the job have been successfully scanned and reported as Clean by Microsoft Defender.
    /// The job is now ready for mandate-specific validation rules to be applied.
    /// </summary>
    AwaitingValidation, // All files are Clean, ready for ValidationRunnerV2

    /// <summary>
    /// Mandate-specific validation rules are currently being applied to one or more files in the job.
    /// </summary>
    Validating, // ValidationRunnerV2 is actively working on this job

    /// <summary>
    /// Job completed successfully: All files were scanned as Clean by Defender,
    /// AND all files passed their mandate-specific validation rules.
    /// The job is now ready for potential delivery.
    /// </summary>
    Completed, // All files Clean and Valid

    /// <summary>
    /// Job terminated due to an issue:
    /// - At least one file was reported as Infected by Defender.
    /// - At least one file failed mandate-specific validation.
    /// - A system error prevented completion (e.g., missing mandate, blob access issues).
    /// See <c>ValidationJob.FailureReason</c> for details.
    /// </summary>
    Failed
}

/// <summary>
/// Fine-grained status of a single file that belongs to a V2 validation job.
/// This tracks the individual file's progress through the system.
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// File entry created in the database, SAS URL generated.
    /// Awaiting client upload.
    /// </summary>
    Pending, // Initial state for a file

    // 'Uploading' is a client-side state. The backend sees it as Pending until /start,
    // then implicitly "UploadedAndQueuedForScan".

    /// <summary>
    /// File has been uploaded by the client (assumption after /start is called for the job).
    /// It is now awaiting its turn to be checked for virus scan results from Microsoft Defender.
    /// The actual scan by Defender might be queued, in progress, or already complete.
    /// Our system considers it "AwaitingScanResult" until we poll and confirm.
    /// </summary>
    AwaitingVirusScanResult, // File is in the job, job is Queued or Scanning. We need to check Defender.

    /// <summary>
    /// Microsoft Defender has reported this file as Clean (no threats found).
    /// It is now awaiting mandate-specific validation if the overall job proceeds.
    /// </summary>
    Clean, // Defender says it's good.

    /// <summary>
    /// Microsoft Defender has reported this file as Infected.
    /// This will typically cause the entire <see cref="ValidationJobStatus"/> to become Failed.
    /// </summary>
    Infected, // Defender says it's bad.

    /// <summary>
    /// The file (already confirmed Clean by Defender) is currently undergoing
    /// mandate-specific validation.
    /// </summary>
    Validating, // Mandate rules are being applied to this specific file.

    /// <summary>
    /// The file (already confirmed Clean by Defender) has passed all
    /// mandate-specific validation rules.
    /// </summary>
    Valid, // Passed mandate rules.

    /// <summary>
    /// The file (already confirmed Clean by Defender) has failed one or more
    /// mandate-specific validation rules.
    /// This will typically cause the entire <see cref="ValidationJobStatus"/> to become Failed.
    /// </summary>
    Invalid, // Failed mandate rules.

    /// <summary>
    /// An error occurred specific to this file during processing (e.g., blob not found when expected,
    /// unexpected error during validation step). This status implies the file could not be fully processed.
    /// </summary>
    ErrorProcessing // A catch-all for file-specific errors not covered by Infected/Invalid
}

/// <summary>
/// Specifies the storage backend used for file persistence and retrieval operations.
/// Determines how files are accessed during validation workflows and where they are stored.
/// </summary>
public enum StorageType
{
    /// <summary>
    /// Files are stored on the local file system of the application server.
    /// </summary>
    LocalFileSystem,

    /// <summary>
    /// Files are stored in Microsoft Azure Blob Storage containers.
    /// </summary>
    AzureBlobStorage
}
