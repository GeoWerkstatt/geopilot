using Asp.Versioning;
using Geopilot.Api.Contracts;
using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;
using Geopilot.Api.Validation.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Geopilot.Api.Controllers.V2;

/// <summary>
/// REST API controller for managing file validation workflows in the V2 validation system.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/validation")]
[AllowAnonymous]
public class ValidationV2Controller : ControllerBase
{
    private readonly ILogger<ValidationV2Controller> logger;
    private readonly IValidationJobService validationJobService;
    private readonly IValidationJobFileService validationJobFileService;
    private readonly IBlobStorageService blobStorageService;
    private readonly AzureBlobStorageOptions blobOptions;
    private readonly IValidationJobLogService validationJobLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationV2Controller"/> class.
    /// </summary>
    /// <param name="logger">Logger for API request tracking and error reporting.</param>
    /// <param name="validationJobService">Service for managing validation job lifecycle and status.</param>
    /// <param name="validationJobFileService">Service for creating and managing file entries within jobs.</param>
    /// <param name="blobStorageService">Service for Azure Blob Storage operations like generating presigned URLs.</param>
    /// <param name="validationJobLogService">Service for managing validation log files.</param>
    /// <param name="blobOptions">Configuration options for Azure Blob Storage.</param>
    public ValidationV2Controller(
        ILogger<ValidationV2Controller> logger,
        IValidationJobService validationJobService,
        IValidationJobFileService validationJobFileService,
        IBlobStorageService blobStorageService,
        IValidationJobLogService validationJobLogService,
        IOptions<AzureBlobStorageOptions> blobOptions)
    {
        this.logger = logger;
        this.validationJobService = validationJobService;
        this.validationJobFileService = validationJobFileService;
        this.blobStorageService = blobStorageService;
        this.blobOptions = blobOptions.Value;
        this.validationJobLogService = validationJobLogService;
    }

    /// <summary>
    /// Creates a new validation job and generates presigned upload URLs for the specified files.
    /// </summary>
    /// <param name="request">Request containing the list of file names to create upload URLs for.</param>
    /// <returns>
    /// A <see cref="UploadUrlsResponse"/> containing the job ID and presigned upload URLs for each file.
    /// </returns>
    /// <response code="201">Validation job created and upload URLs generated successfully.</response>
    /// <response code="400">If the request is invalid or the list of files is empty.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("upload-url")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request payload.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error creating job or generating URLs.")]
    [SwaggerResponse(StatusCodes.Status201Created,
        "Validation job created and upload URLs generated.",
        typeof(UploadUrlsResponse)
    )]
    public async Task<IActionResult> CreateJobWithUploads([FromBody] UploadUrlsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.FileNames.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "FileNames cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var job = await validationJobService.CreateValidationJobAsync();

            var response = await CreateFilesAndGenerateUrlsAsync(job.Id, request.FileNames);

            logger.LogInformation("Created job {JobId} with {UrlCount} upload URLs", job.Id, response.UploadUrls.Count);
            return CreatedAtAction(nameof(GetJobStatus), new { version = "2.0", jobId = job.Id }, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating validation job with uploads.");
            return Problem(
                "An error occurred while creating the validation job and upload URLs.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Adds additional files to an existing validation job and generates presigned upload URLs.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validation job to add files to.</param>
    /// <param name="request">Request containing the list of additional file names to add.</param>
    /// <returns>
    /// A <see cref="UploadUrlsResponse"/> containing the job ID and presigned upload URLs for the new files.
    /// </returns>
    /// <response code="200">Files added and upload URLs generated successfully.</response>
    /// <response code="400">If the request is invalid or the list of files is empty.</response>
    /// <response code="404">If the validation job is not found.</response>
    [HttpPost("{jobId}/add-files")]
    [SwaggerResponse(StatusCodes.Status200OK, "Files added and upload URLs generated.", typeof(UploadUrlsResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request payload.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Validation job not found.")]
    public async Task<IActionResult> AddFilesToJob(Guid jobId, [FromBody] UploadUrlsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.FileNames.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "FileNames cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // Generate URLs for existing job
            var response = await CreateFilesAndGenerateUrlsAsync(jobId, request.FileNames);

            logger.LogInformation("Added {UrlCount} upload URLs to existing job {JobId}", response.UploadUrls.Count,
                jobId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request for job {JobId}", jobId);
            return NotFound(new ProblemDetails
            {
                Title = "Validation job not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding files to job {JobId}", jobId);
            return Problem(
                $"An error occurred while adding files to job {jobId}.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Initiates the validation process for a job after all files have been uploaded.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validation job to start processing.</param>
    /// <param name="request">Validation request containing mandate ID and processing configuration.</param>
    /// <returns>
    /// A <see cref="ValidationResponse"/> with the job status.
    /// </returns>
    /// <response code="202">Validation process queued successfully for background processing.</response>
    /// <response code="400">If the request is invalid, or the job cannot be started in its current state.</response>
    /// <response code="404">If the validation job is not found.</response>
    [HttpPost("{jobId}/start")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request or job cannot be started.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Validation job not found.")]
    [SwaggerResponse(
        StatusCodes.Status202Accepted,
        "Validation process queued successfully.",
        typeof(ValidationResponse)
    )]
    public async Task<IActionResult> StartValidation(Guid jobId, [FromBody] ValidationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await validationJobService.StartValidationAsync(jobId, request);
            if (response == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Validation job not found",
                    Detail = $"Job with ID {jobId} not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return AcceptedAtAction(nameof(GetJobStatus), new { version = "2.0", jobId }, response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request for starting validation job {JobId}", jobId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request to start validation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot start validation for job {JobId}", jobId);
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot start validation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting validation for job {JobId}", jobId);
            return Problem(
                $"An error occurred while starting validation for job {jobId}.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Retrieves the current status and validation results for a validation job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validation job to query.</param>
    /// <returns>
    /// A <see cref="ValidationStatusResponse"/> containing job status and file processing details.
    /// </returns>
    /// <response code="200">The current status and results of the validation job.</response>
    /// <response code="404">If the validation job is not found.</response>
    [HttpGet("{jobId}")]
    [ActionName(nameof(GetJobStatus))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Validation job not found.")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Current status of the validation job.",
        typeof(ValidationStatusResponse)
    )]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        try
        {
            var status = await validationJobService.GetJobStatusAsync(jobId);
            if (status == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Validation job not found",
                    Detail = $"Job with ID {jobId} not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving status for job {JobId}", jobId);
            return Problem(
                $"An error occurred while retrieving job {jobId} status.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Downloads a validation log file by its unique identifier.
    /// </summary>
    /// <param name="logId">The unique identifier of the validation log to download.</param>
    /// <returns>
    /// The log file content as a downloadable stream with a meaningful filename (e.g., "original-file_validator.log").
    /// </returns>
    /// <response code="200">The log file content as a text/plain stream.</response>
    /// <response code="404">If the validation log is not found.</response>
    [HttpGet("logs/{logId}")]
    [Produces("text/plain")]
    [SwaggerResponse(StatusCodes.Status200OK, "Log file downloaded successfully.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Validation log not found.")]
    public async Task<IActionResult> DownloadLog(Guid logId)
    {
        try
        {
            var logStream = await validationJobLogService.GetLogContentAsync(logId);
            if (logStream == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Validation log not found",
                    Detail = $"Log with ID {logId} not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Generate a meaningful filename based on the log's associated file and name.
            var logInfo = await validationJobLogService.GetLogInfoAsync(logId);
            var fileName = logInfo != null
                ? $"{logInfo.ValidationJobFile.OriginalFileName}_{logInfo.LogName}.log"
                : $"validation_log_{logId}.log";

            return File(logStream, "text/plain", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading validation log {LogId}", logId);
            return Problem(
                $"An error occurred while downloading log {logId}.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Helper method to create file entries and generate upload URLs.
    /// </summary>
    /// <param name="jobId">The validation job ID to associate the files with.</param>
    /// <param name="fileNames">A list of file names to create entries for.</param>
    /// <returns>An <see cref="UploadUrlsResponse"/> with the job ID and generated URLs.</returns>
    private async Task<UploadUrlsResponse> CreateFilesAndGenerateUrlsAsync(Guid jobId, IEnumerable<string> fileNames)
    {
        var files = await validationJobFileService.CreateFileEntriesAsync(
            jobId,
            fileNames,
            StorageType.AzureBlobStorage
        );

        var expiry = TimeSpan.FromMinutes(blobOptions.PresignedUrlExpiryMinutes);
        var uploadUrls = new List<UploadUrl>();

        foreach (var file in files)
        {
            var url = await blobStorageService.GeneratePresignedUploadUrlAsync(file.Location, expiry);
            uploadUrls.Add(new UploadUrl
            {
                FileName = file.OriginalFileName,
                Url = url,
                ExpiresAt = DateTime.UtcNow.Add(expiry)
            });
        }

        return new UploadUrlsResponse
        {
            ValidationJobId = jobId,
            UploadUrls = uploadUrls
        };
    }
}
