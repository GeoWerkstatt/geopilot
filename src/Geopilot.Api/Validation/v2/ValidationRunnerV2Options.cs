namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Configuration options for the <see cref="ValidationRunnerV2"/> background service.
/// </summary>
public class ValidationRunnerV2Options
{
    /// <summary>
    /// Gets or sets the interval in seconds between validation job processing cycles.
    /// </summary>
    /// <value>
    /// The number of seconds to wait between checking for new validation jobs to process.
    /// Defaults to 10 seconds if not configured.
    /// </value>
    /// <example>
    /// <code>
    /// // Fast polling for development
    /// PollIntervalSeconds = 2;
    ///
    /// // Balanced polling for production
    /// PollIntervalSeconds = 10;
    ///
    /// // Conservative polling for high-load systems
    /// PollIntervalSeconds = 30;
    /// </code>
    /// </example>
    public int PollIntervalSeconds { get; set; } = 10;
}
