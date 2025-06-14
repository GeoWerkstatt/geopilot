using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geopilot.Api.Models;

/// <summary>
/// Represents a validation log file generated during the processing of a validation job file,
/// tracking its storage location and type for subsequent retrieval and download.
/// </summary>
public class ValidationJobLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation log file.
    /// </summary>
    /// <value>
    /// An auto-incrementing integer that serves as the primary key for database operations.
    /// </value>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the validation job file that generated this log.
    /// </summary>
    [Required]
    public int ValidationJobFileId { get; set; }

    /// <summary>
    /// Gets or sets the validation job file that generated this log.
    /// </summary>
    /// <value>
    /// The parent <see cref="ValidationJobFile"/> entity that underwent validation
    /// and produced this log as part of the validation process.
    /// </value>
    [ForeignKey("ValidationJobFileId")]
    public ValidationJobFile ValidationJobFile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type or category of this validation log.
    /// </summary>
    /// <value>
    /// A string identifying the type of log content, such as "Log", "Xtf-Log", "ErrorReport", etc.
    /// This value is determined by the validator and used to distinguish between different
    /// types of output from the same validation process.
    /// </value>
    [Required]
    [MaxLength(100)]
    public string LogName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage location identifier for this log file.
    /// </summary>
    /// <value>
    /// A storage-backend-specific identifier that uniquely locates the log file content.
    /// Format depends on the <see cref="StorageType"/>:
    /// <list type="bullet">
    /// <item><term>LocalFileSystem:</term><description>Full file system path</description></item>
    /// <item><term>AzureBlobStorage:</term><description>Blob name within the configured container</description></item>
    /// </list>
    /// </value>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage backend used to persist this log file.
    /// </summary>
    /// <value>
    /// The <see cref="StorageType"/> that determines how the log file is accessed
    /// and what storage-specific features are available.
    /// </value>
    [Required]
    public StorageType StorageType { get; set; }
}
