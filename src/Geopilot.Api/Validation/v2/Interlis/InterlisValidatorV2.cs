using System.Net;
using System.Text.Json;
using Geopilot.Api.Validation.Interlis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Geopilot.Api.Validation.V2.Interlis;

/// <summary>
/// INTERLIS format validator that validates geospatial transfer files against INTERLIS standards.
/// Supports both .xtf (XML Transfer Format) and .itf (INTERLIS Transfer Format) files through
/// integration with an external INTERLIS validation service.
/// </summary>
public class InterlisValidatorV2 : IValidatorV2
{
    private const string UploadUrl = "/api/v1/upload";
    private const string SettingsUrl = "/api/v1/settings";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int MaxPolls = 60;

    private readonly ILogger<InterlisValidatorV2> logger;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly JsonSerializerOptions jsonOptions;

    /// <inheritdoc />
    public string Name => "INTERLIS";

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlisValidatorV2"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking validation operations, errors, and performance metrics.</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients configured for the INTERLIS validation service.</param>
    /// <param name="jsonOptions">JSON serialization options for consistent API communication with the validation service.</param>
    public InterlisValidatorV2(
        ILogger<InterlisValidatorV2> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<JsonOptions> jsonOptions)
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> GetSupportedFileExtensionsAsync(CancellationToken stoppingToken)
    {
        var client = httpClientFactory.CreateClient("INTERLIS_VALIDATOR_HTTP_CLIENT");
        var response = await client.GetAsync(SettingsUrl, stoppingToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<InterlisSettingsResponse>(
            jsonOptions,
            stoppingToken
        ).ConfigureAwait(false);

        return config?.AcceptedFileTypes?.Split(", ", StringSplitOptions.RemoveEmptyEntries) ?? [];
    }

    /// <inheritdoc />
    public async Task<ValidatorV2Result> ExecuteAsync(Stream file, string fileName, CancellationToken stoppingToken)
    {
        logger.LogInformation("V2 INTERLIS start: '{FileName}'", fileName);

        var extensionResult = await ValidateFileExtensionAsync(fileName, stoppingToken);
        if (extensionResult != null)
            return extensionResult;

        var statusUrl = await UploadToValidatorAsync(file, fileName, stoppingToken);
        if (statusUrl == null)
        {
            return CreateFailureResult("Upload failed - no status URL returned");
        }

        return await PollForCompletionAsync(statusUrl, stoppingToken);
    }

    /// <summary>
    /// Performs a pre-check to ensure the file extension is supported by querying the external validation service
    /// for its current capabilities. This ensures consistency between routing logic and validation logic.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <param name="stoppingToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A validation result if the extension is unsupported; otherwise, null to proceed with validation.</returns>
    private async Task<ValidatorV2Result?> ValidateFileExtensionAsync(string fileName, CancellationToken stoppingToken)
    {
        var supportedExtensions = await GetSupportedFileExtensionsAsync(stoppingToken);
        var fileExtension = Path.GetExtension(fileName);

        if (!supportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidatorV2Result
            {
                Status = Status.CompletedWithErrors,
                Message = $"Extension '{fileExtension}' not supported",
                Logs = null
            };
        }
        return null;
    }

    /// <summary>
    /// Uploads the file stream to the external INTERLIS validation service.
    /// </summary>
    /// <param name="fileStream">The stream containing the file content.</param>
    /// <param name="fileName">The original filename.</param>
    /// <param name="stoppingToken">A cancellation token.</param>
    /// <returns>The status URL for polling the validation result, or null if the upload fails.</returns>
    private async Task<string?> UploadToValidatorAsync(Stream fileStream, string fileName, CancellationToken stoppingToken)
    {
        var client = httpClientFactory.CreateClient("INTERLIS_VALIDATOR_HTTP_CLIENT");
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(fileStream), "file", fileName);

        try
        {
            var uploadResponse = await client.PostAsync(UploadUrl, form, stoppingToken).ConfigureAwait(false);

            if (uploadResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                var problemDetails = await uploadResponse.Content
                    .ReadFromJsonAsync<ValidationProblemDetails>(jsonOptions, stoppingToken)
                    .ConfigureAwait(false);
                logger.LogWarning("Upload rejected: {Detail}", problemDetails?.Detail ?? "Invalid transfer file");
                return null;
            }

            uploadResponse.EnsureSuccessStatusCode();

            var uploadData = await uploadResponse.Content
                .ReadFromJsonAsync<InterlisUploadResponse>(jsonOptions, stoppingToken)
                .ConfigureAwait(false);

            return uploadData?.StatusUrl;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload to Interlis failed for file '{FileName}'", fileName);
            return null;
        }
    }

    /// <summary>
    /// Polls the given status URL until the validation is complete, times out, or is cancelled.
    /// </summary>
    /// <param name="statusUrl">The URL to poll for status updates.</param>
    /// <param name="stoppingToken">A cancellation token.</param>
    /// <returns>The final validation result.</returns>
    private async Task<ValidatorV2Result> PollForCompletionAsync(string statusUrl, CancellationToken stoppingToken)
    {
        var client = httpClientFactory.CreateClient("INTERLIS_VALIDATOR_HTTP_CLIENT");
        var pollCount = 0;

        while (!stoppingToken.IsCancellationRequested && pollCount++ < MaxPolls)
        {
            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);

            try
            {
                var pollResp = await client.GetAsync(statusUrl, stoppingToken).ConfigureAwait(false);
                pollResp.EnsureSuccessStatusCode();

                var statusData = await pollResp.Content
                    .ReadFromJsonAsync<InterlisStatusResponse>(jsonOptions, stoppingToken)
                    .ConfigureAwait(false);

                if (statusData != null && statusData.Status != Status.Processing)
                {
                    return await CreateResultFromStatusAsync(statusData, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error polling validation status from {StatusUrl}", statusUrl);
                return CreateFailureResult($"Error checking validation status: {ex.Message}");
            }
        }

        var timeoutMessage = $"Validation timed out after {MaxPolls} polls ({PollInterval.TotalSeconds * MaxPolls:N0}s).";
        logger.LogWarning("Validation timeout for status URL: {StatusUrl}", statusUrl);
        return CreateFailureResult(timeoutMessage);
    }

    /// <summary>
    /// Creates a final <see cref="ValidatorV2Result"/> from a completed status response.
    /// </summary>
    /// <param name="statusData">The status response from the validation service.</param>
    /// <param name="stoppingToken">A cancellation token.</param>
    /// <returns>The mapped validation result.</returns>
    private async Task<ValidatorV2Result> CreateResultFromStatusAsync(InterlisStatusResponse statusData, CancellationToken stoppingToken)
    {
        var logs = await CollectLogsAsync(statusData, stoppingToken);

        return new ValidatorV2Result
        {
            Status = statusData.Status,
            Message = statusData.StatusMessage ?? string.Empty,
            Logs = logs
        };
    }
}
