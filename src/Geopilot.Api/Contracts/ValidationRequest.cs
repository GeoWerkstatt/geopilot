namespace Geopilot.Api.Contracts;

/// <summary>
/// Request to move a job from “pending” into the validation queue,
/// specifying which mandate’s rules should be applied.
/// </summary>
public sealed class ValidationRequest
{
    /// <summary>
    /// Foreign-key of the mandate to validate against.
    /// </summary>
    public int MandateId { get; set; }
}
