namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Defines a contract for file format validators in the V2 validation pipeline.
/// </summary>
public interface IValidatorV2
{
    /// <summary>
    /// Gets the human-readable name identifier for this validator.
    /// </summary>
    /// <value>
    /// A unique string that identifies this validator implementation for logging,
    /// debugging, and user interface purposes.
    /// </value>
    string Name { get; }

    /// <summary>
    /// Retrieves the collection of file extensions that this validator can process.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token for the asynchronous operation, particularly important
    /// for validators that query external services for capability information.
    /// </param>
    /// <returns>
    /// A collection of file extensions (including the leading dot) that this validator
    /// supports, or an empty collection if the validator is unavailable or misconfigured.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Static capability validator
    /// public async Task&lt;ICollection&lt;string&gt;&gt; GetSupportedFileExtensionsAsync(CancellationToken cancellationToken)
    /// {
    ///     return new[] { ".xtf", ".itf" };
    /// }
    ///
    /// // Dynamic capability validator
    /// public async Task&lt;ICollection&lt;string&gt;&gt; GetSupportedFileExtensionsAsync(CancellationToken cancellationToken)
    /// {
    ///     try
    ///     {
    ///         var config = await validationService.GetCapabilitiesAsync(cancellationToken);
    ///         return config.SupportedExtensions;
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         logger.LogWarning(ex, "Could not retrieve validator capabilities");
    ///         return Array.Empty&lt;string&gt;();
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<ICollection<string>> GetSupportedFileExtensionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes validation logic against the provided file stream and returns detailed results.
    /// </summary>
    /// <param name="fileStream">
    /// The stream containing the file data to validate. Must be positioned at the beginning
    /// and remain readable throughout the validation process. The validator should not
    /// dispose this stream.
    /// </param>
    /// <param name="fileName">
    /// The original filename including extension. Used for context, logging, and may influence
    /// validation behavior based on file naming conventions or embedded metadata.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to abort long-running validation operations. Validators should
    /// check this token periodically during processing and return promptly when cancellation
    /// is requested.
    /// </param>
    /// <returns>
    /// A <see cref="ValidatorV2Result"/> containing the validation outcome, diagnostic information,
    /// error/warning counts, and references to detailed log files.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fileStream"/> or <paramref name="fileName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileName"/> is empty or contains only whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// Note: Prefer returning a failed <see cref="ValidatorV2Result"/> over throwing this exception
    /// when cancellation occurs during validation processing.
    /// </exception>
    /// <example>
    /// <code>
    /// public async Task&lt;ValidatorV2Result&gt; ExecuteAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    /// {
    ///     logger.LogInformation("Starting validation of {FileName}", fileName);
    ///
    ///     try
    ///     {
    ///         // Perform validation logic
    ///         var validationOutcome = await ValidateFileContent(fileStream, cancellationToken);
    ///
    ///         return new ValidatorV2Result
    ///         {
    ///             Status = validationOutcome.IsValid ? Status.Completed : Status.CompletedWithErrors,
    ///             Message = validationOutcome.Summary,
    ///             ErrorCount = validationOutcome.Errors.Count,
    ///             WarningCount = validationOutcome.Warnings.Count,
    ///             LogUrls = await SaveValidationLogs(validationOutcome.DetailedLogs)
    ///         };
    ///     }
    ///     catch (Exception ex) when (!(ex is OperationCanceledException))
    ///     {
    ///         logger.LogError(ex, "Validation failed for {FileName}", fileName);
    ///
    ///         return new ValidatorV2Result
    ///         {
    ///             Status = Status.Failed,
    ///             Message = $"Validation error: {ex.Message}",
    ///             ErrorCount = 1,
    ///             WarningCount = 0,
    ///             LogUrls = null
    ///         };
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<ValidatorV2Result> ExecuteAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);
}
