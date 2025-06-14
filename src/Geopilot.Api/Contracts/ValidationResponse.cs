using Geopilot.Api.Models;

namespace Geopilot.Api.Contracts;

/// <summary>
/// Minimal acknowledgement once the job is queued for background processing.
/// </summary>
public sealed class ValidationResponse
{
    /// <summary/>
    public Guid ValidationJobId { get; set; }

    /// <summary>
    /// Initial status after queueing – should be <see cref="ValidationJobStatus.Queued"/>.
    /// </summary>
    public ValidationJobStatus ValidationJobStatus { get; set; }
}
