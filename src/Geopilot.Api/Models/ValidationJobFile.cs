using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geopilot.Api.Models;

/// <summary>
/// Represents an individual file that is part of a validation job, tracking its
/// processing status, storage location, and validation results.
/// </summary>
public class ValidationJobFile
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation job file.
    /// </summary>
    /// <value>
    /// An auto-incrementing integer that serves as the primary key for database operations.
    /// </value>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the validation job that contains this file.
    /// </summary>
    [Required]
    public Guid ValidationJobId { get; set; }

    /// <summary>
    /// Gets or sets the validation job that contains this file.
    /// </summary>
    /// <value>
    /// The parent <see cref="ValidationJob"/> entity that orchestrates the processing
    /// of this file along with any other files in the same batch.
    /// </value>
    [ForeignKey("ValidationJobId")]
    public ValidationJob ValidationJob { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original filename as provided by the user during upload.
    /// </summary>
    /// <value>
    /// The filename including extension as it existed on the user's system.
    /// This value is preserved for user reference and file type detection.
    /// </value>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage location identifier for this file.
    /// </summary>
    /// <value>
    /// A storage-backend-specific identifier that uniquely locates the file content.
    /// Format depends on the <see cref="StorageType"/>:
    /// <list type="bullet">
    /// <item><term>LocalFileSystem:</term><description>Full file system path</description></item>
    /// <item><term>AzureBlobStorage:</term><description>Blob name within the configured container</description></item>
    /// </list>
    /// </value>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage backend used to persist this file.
    /// </summary>
    /// <value>
    /// The <see cref="StorageType"/> that determines how the file is accessed
    /// and what storage-specific features (like virus scanning) are available.
    /// </value>
    [Required]
    public StorageType StorageType { get; set; }

    /// <summary>
    /// Gets or sets the current processing status of this individual file.
    /// </summary>
    /// <value>
    /// The current stage of processing for this specific file. Defaults to <see cref="FileStatus.Pending"/>.
    /// File status progresses independently of other files in the same job.
    /// </value>
    [Required]
    [MaxLength(64)]
    public FileStatus FileStatus { get; set; } = FileStatus.Pending;

    /// <summary>
    /// Gets or sets the detailed validation results for this file as a JSON string.
    /// </summary>
    /// <value>
    /// A JSON-serialized object containing validation results, error details, warning counts,
    /// and any additional metadata from the validation process, or <c>null</c> if validation
    /// has not yet been performed.
    /// </value>
    public string? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this file was successfully uploaded to storage.
    /// </summary>
    /// <value>
    /// The UTC date and time when the file upload completed, or <c>null</c> if the
    /// upload timestamp is unknown or the file upload failed.
    /// </value>
    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the size of the uploaded file in bytes.
    /// </summary>
    /// <value>
    /// The exact file size in bytes, or <c>null</c> if the size is unknown.
    /// This value is typically retrieved from storage metadata.
    /// </value>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the collection of logs associated with this validation file.
    /// </summary>
    /// <value>
    /// A list of <see cref="ValidationJobLog"/> entities representing all logs
    /// created after processing this job. Defaults to an empty list.
    /// </value>
    public List<ValidationJobLog> Logs { get; set; } = new();
}
