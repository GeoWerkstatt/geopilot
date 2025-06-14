namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Represents the outcome of a file validation operation performed by a V2 validator.
/// </summary>
public class ValidatorV2Result
{
    /// <summary>
    /// Gets the overall outcome status of the validation operation.
    /// </summary>
    /// <value>
    /// A <see cref="Status"/> value indicating whether validation completed successfully,
    /// failed due to data issues, or encountered system-level errors.
    /// </value>
    public Status Status { get; init; }

    /// <summary>
    /// Gets a human-readable summary message describing the validation outcome.
    /// </summary>
    /// <value>
    /// A localized message suitable for display to end users, or an empty string if no specific message is available.
    /// </value>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets a collection of URLs or identifiers pointing to detailed validation log files.
    /// </summary>
    /// <value>
    /// A dictionary mapping log type names to their corresponding URLs or file identifiers,
    /// or <c>null</c> if no detailed logs are available.
    /// </value>
    public Dictionary<string, string>? Logs { get; init; }
}
