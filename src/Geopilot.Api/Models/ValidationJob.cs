using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geopilot.Api.Models;

/// <summary>
/// Represents a validation job that processes one or more uploaded files through
/// security scanning and format validation workflows.
/// </summary>
public class ValidationJob
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation job.
    /// </summary>
    /// <value>
    /// A globally unique identifier (GUID) that serves as the primary key
    /// and can be safely exposed in APIs and user interfaces.
    /// </value>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the current processing status of the validation job.
    /// </summary>
    /// <value>
    /// The current stage in the validation workflow. Defaults to <see cref="ValidationJobStatus.Pending"/>.
    /// Status transitions are managed by background processing services.
    /// </value>
    [Required]
    [MaxLength(64)]
    public ValidationJobStatus Status { get; set; } = ValidationJobStatus.Pending;

    /// <summary>
    /// Gets or sets the identifier of the mandate that governs this validation job's requirements.
    /// </summary>
    /// <value>
    /// The foreign key to the associated <see cref="Mandate"/>, or <c>null</c> if no specific
    /// mandate requirements apply. When set, determines which validation rules and
    /// file format requirements will be enforced.
    /// </value>
    public int? MandateId { get; set; }

    /// <summary>
    /// Gets or sets the mandate that defines validation requirements for this job.
    /// </summary>
    /// <value>
    /// The <see cref="Mandate"/> entity that specifies validation rules, file format requirements,
    /// and compliance standards, or <c>null</c> if using default validation behavior.
    /// </value>
    [ForeignKey("MandateId")]
    public Mandate? Mandate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this validation job was created.
    /// </summary>
    /// <value>
    /// The UTC date and time when the job was first created in the system.
    /// Defaults to the current UTC time when the entity is instantiated.
    /// </value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this validation job completed processing.
    /// </summary>
    /// <value>
    /// The UTC date and time when the job reached a final state (Completed or Failed),
    /// or <c>null</c> if the job is still in progress.
    /// </value>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason why this validation job failed, if applicable.
    /// </summary>
    /// <value>
    /// A human-readable description of what caused the job to fail, or <c>null</c>
    /// if the job completed successfully or is still in progress.
    /// </value>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the collection of files associated with this validation job.
    /// </summary>
    /// <value>
    /// A list of <see cref="ValidationJobFile"/> entities representing all files
    /// uploaded for processing in this job. Defaults to an empty list.
    /// </value>
    public List<ValidationJobFile> Files { get; set; } = new();
}
