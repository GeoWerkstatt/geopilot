using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Geopilot.Api.Validation.V2;

/// <inheritdoc />
public class VirusScanService : IVirusScanService
{
    private readonly Context context;
    private readonly IBlobStorageService blobStorageService;
    private readonly ILogger<VirusScanService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirusScanService"/> class.
    /// </summary>
    /// <param name="context">
    /// The Entity Framework database context for accessing validation jobs and file entities.
    /// Used for querying job status and updating file scan results.
    /// </param>
    /// <param name="blobStorageService">
    /// Service for interacting with Azure Blob Storage, including virus scan status queries
    /// and blob existence checks. Required for Azure storage integration.
    /// </param>
    /// <param name="logger">
    /// Logger for tracking virus scan operations, file status transitions, and error conditions.
    /// Essential for debugging scan issues and monitoring security processing.
    /// </param>
    public VirusScanService(Context context, IBlobStorageService blobStorageService, ILogger<VirusScanService> logger)
    {
        this.context = context;
        this.blobStorageService = blobStorageService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessJobForVirusScansAsync(Guid validationJobId)
    {
        var job = await LoadJobAsync(validationJobId);
        if (job == null)
        {
            return;
        }

        EnsureJobScanningState(job);

        var filesToProcess = job.Files
            .Where(file => file.FileStatus == FileStatus.AwaitingVirusScanResult || file.FileStatus == FileStatus.Pending)
            .ToList();

        if (!filesToProcess.Any())
        {
            await HandleNoFilesToProcessAsync(job);
        }
        else
        {
            await HandleFilesToProcessAsync(job, filesToProcess);
        }
    }

    private async Task<Models.ValidationJob?> LoadJobAsync(Guid jobId)
    {
        var job = await context.ValidationJobs
            .Include(job => job.Files)
            .FirstOrDefaultAsync(job => job.Id == jobId);

        if (job == null
            || (job.Status != Models.ValidationJobStatus.Queued &&
                job.Status != Models.ValidationJobStatus.AwaitingVirusScanResults))
        {
            logger.LogTrace("Job {JobId} not eligible for virus scan (Status: {Status})", jobId, job?.Status);
            return null;
        }

        return job;
    }

    private void EnsureJobScanningState(Models.ValidationJob job)
    {
        if (job.Status == Models.ValidationJobStatus.Queued)
        {
            job.Status = Models.ValidationJobStatus.AwaitingVirusScanResults;
            context.SaveChanges();
        }
    }

    private async Task HandleNoFilesToProcessAsync(Models.ValidationJob job)
    {
        var allFiles = await context.ValidationJobFiles
            .Where(file => file.ValidationJobId == job.Id)
            .ToListAsync();

        if (!allFiles.Any())
        {
            FailJob(job, "No files were provided for validation.");
        }
        else if (allFiles.Any(f => f.FileStatus is FileStatus.Infected or FileStatus.ErrorProcessing))
        {
            FailJob(job, "One or more files are infected or errored.");
        }
        else if (allFiles.All(f => f.FileStatus == FileStatus.Clean))
        {
            job.Status = Models.ValidationJobStatus.AwaitingValidation;
            await context.SaveChangesAsync();
        }
    }

    private async Task HandleFilesToProcessAsync(Models.ValidationJob job, List<ValidationJobFile> files)
    {
        bool sawError = false;
        bool sawInfected = false;
        bool stillWaiting = false;

        foreach (var file in files)
        {
            var result = await ProcessSingleFileScanAsync(file);
            sawError |= result.hadError;
            sawInfected |= result.wasInfected;
            stillWaiting |= result.stillWaiting;
        }

        if (sawInfected || sawError)
        {
            FailJob(job, sawInfected ? "One or more files failed virus scan." : "Error processing one or more files.");
        }
        else if (stillWaiting)
        {
            job.Status = Models.ValidationJobStatus.AwaitingVirusScanResults;
            await context.SaveChangesAsync();
        }
        else
        {
            job.Status = Models.ValidationJobStatus.AwaitingValidation;
            await context.SaveChangesAsync();
        }
    }

    private async Task<(bool hadError, bool wasInfected, bool stillWaiting)> ProcessSingleFileScanAsync(
        ValidationJobFile file)
    {
        if (file.StorageType != StorageType.AzureBlobStorage)
        {
            logger.LogDebug(
                "Skipping virus scan for file {FileId} - storage type {StorageType} not supported",
                file.Id,
                file.StorageType
            );

            // For non-Azure storage, assume clean and proceed to validation
            if (file.FileStatus == FileStatus.Pending || file.FileStatus == FileStatus.AwaitingVirusScanResult)
            {
                file.FileStatus = FileStatus.Clean;
                await context.SaveChangesAsync();
            }

            return (hadError: false, wasInfected: false, stillWaiting: false);
        }

        if (!await blobStorageService.BlobExistsAsync(file.Location))
        {
            logger.LogWarning("Missing blob for File {FileId}.", file.Id);
            file.FileStatus = FileStatus.ErrorProcessing;
            await context.SaveChangesAsync();
            return (hadError: true, wasInfected: false, stillWaiting: false);
        }

        if (file.UploadedAt == null)
        {
            var props = await blobStorageService.GetBlobPropertiesAsync(file.Location);
            if (props != null)
            {
                file.UploadedAt = props.CreatedOn.UtcDateTime;
                file.FileSizeBytes = props.ContentLength;
            }
        }

        var defenderStatus = await blobStorageService.GetVirusScanStatusAsync(file.Location);
        file.FileStatus = defenderStatus;

        bool hadError = false;
        bool wasInfected = false;
        bool stillWaiting = false;

        switch (defenderStatus)
        {
            case FileStatus.Clean:
                break;

            case FileStatus.Infected:
                wasInfected = true;
                break;

            case FileStatus.AwaitingVirusScanResult:
                stillWaiting = true;
                break;

            default:
                hadError = true;
                break;
        }

        return (hadError, wasInfected, stillWaiting);
    }

    private void FailJob(Models.ValidationJob job, string reason)
    {
        job.Status = Models.ValidationJobStatus.Failed;
        job.FailureReason = reason;
        job.CompletedAt = DateTime.UtcNow;
        context.SaveChanges();
        logger.LogWarning("Job {JobId} failed: {Reason}", job.Id, reason);
    }
}
