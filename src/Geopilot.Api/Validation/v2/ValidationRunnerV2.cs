using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Background service that continuously processes validation jobs through their complete lifecycle.
/// Handles virus scanning status checks, file validation using appropriate validators, and job status updates.
/// </summary>
public class ValidationRunnerV2 : BackgroundService
{
    private readonly ILogger<ValidationRunnerV2> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ValidationRunnerV2Options options;
    private readonly IBlobStorageService blobStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRunnerV2"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking validation operations and errors.</param>
    /// <param name="serviceProvider">Service provider for creating scoped database contexts during job processing.</param>
    /// <param name="options">Configuration options including polling intervals and processing behavior.</param>
    /// <param name="blobStorageService">Service for accessing files stored in Azure Blob Storage.</param>
    public ValidationRunnerV2(
        ILogger<ValidationRunnerV2> logger,
        IServiceProvider serviceProvider,
        IOptions<ValidationRunnerV2Options> options,
        IBlobStorageService blobStorageService
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.options = options.Value;
        this.blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Executes the main validation processing loop as a background service.
    /// Continuously polls for pending validation jobs and processes them until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that signals when the service should stop processing.</param>
    /// <returns>A task that represents the background service execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ValidationRunnerV2 starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ValidationRunnerV2 main loop");
            }

            await DelayAsync(stoppingToken);
        }

        logger.LogInformation("ValidationRunnerV2 stopped");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Context>();
        var virusScanService = scope.ServiceProvider.GetRequiredService<IVirusScanService>();
        var validators = scope.ServiceProvider.GetRequiredService<IEnumerable<IValidatorV2>>().ToList();

        var jobs = await GetJobsToProcessAsync(context, stoppingToken);

        foreach (var job in jobs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            logger.LogInformation("Processing job {JobId} with status {Status}", job.Id, job.Status);

            try
            {
                await ProcessSingleJobAsync(job, context, virusScanService, validators, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing job {JobId}", job.Id);
                await FailJobAsync(job, context, $"Processing error: {ex.Message}", stoppingToken);
            }
        }
    }

    private async Task<List<Models.ValidationJob>> GetJobsToProcessAsync(Context context,
        CancellationToken stoppingToken)
    {
        return await context.ValidationJobs
            .Where(job => job.Status == Models.ValidationJobStatus.Queued
                          || job.Status == Models.ValidationJobStatus.AwaitingVirusScanResults
                          || job.Status == Models.ValidationJobStatus.AwaitingValidation)
            .Include(j => j.Files)
            .ToListAsync(stoppingToken);
    }

    private async Task ProcessSingleJobAsync(Models.ValidationJob job, Context context,
        IVirusScanService virusScanService, IList<IValidatorV2> validators, CancellationToken stoppingToken)
    {
        // Check if virus scan complete, update validation status accordingly
        if (job.Status == Models.ValidationJobStatus.Queued ||
            job.Status == Models.ValidationJobStatus.AwaitingVirusScanResults)
        {
            await virusScanService.ProcessJobForVirusScansAsync(job.Id);
            await context.Entry(job).ReloadAsync(stoppingToken);
        }

        // Validate if all files clean
        if (job.Status == Models.ValidationJobStatus.AwaitingValidation)
        {
            if (!job.MandateId.HasValue)
            {
                await FailJobAsync(job, context, "MandateId not set for validation", stoppingToken);
                return;
            }

            await ValidateJobFilesAsync(job, context, validators, stoppingToken);
        }
    }

    private async Task ValidateJobFilesAsync(Models.ValidationJob job, Context context,
        IList<IValidatorV2> validators, CancellationToken stoppingToken)
    {
        job.Status = Models.ValidationJobStatus.Validating;
        await context.SaveChangesAsync(stoppingToken);
        var cleanFiles = job.Files.Where(file => file.FileStatus == FileStatus.Clean).ToList();
        if (!cleanFiles.Any())
        {
            await DetermineFinalJobStatusAsync(job, context, stoppingToken);
            return;
        }

        var failureReasons = new List<string>();

        foreach (var file in cleanFiles)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var success = await ValidateSingleFileAsync(file, job, context, validators, stoppingToken);

            if (!success)
            {
                failureReasons.Add($"File '{file.OriginalFileName}' failed validation");
            }
        }

        if (failureReasons.Any())
        {
            await FailJobAsync(job, context, string.Join("; ", failureReasons), stoppingToken);
        }
        else
        {
            await DetermineFinalJobStatusAsync(job, context, stoppingToken);
        }
    }

    private async Task<bool> ValidateSingleFileAsync(
        ValidationJobFile file,
        Models.ValidationJob job,
        Context context,
        IEnumerable<IValidatorV2> validators,
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Validating file {FileName} for job {JobId}", file.OriginalFileName, job.Id);

        try
        {
            var validator = await GetValidatorAsync(file, validators, stoppingToken);
            if (validator == null)
                return await SetFileErrorAsync(file, context,
                    $"No validator for '{Path.GetExtension(file.OriginalFileName)}'", stoppingToken);

            await using var fileStream = await GetFileStreamAsync(file, stoppingToken);
            if (fileStream == null)
                return await SetFileErrorAsync(file, context, "Unable to access file", stoppingToken);

            await SetFileStatusAsync(file, context, FileStatus.Validating, stoppingToken);

            var result = await validator.ExecuteAsync(fileStream, file.OriginalFileName, stoppingToken);

            return await ProcessValidationResultAsync(file, context, result, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating file {FileName}", file.OriginalFileName);
            return await SetFileErrorAsync(file, context, ex.Message, stoppingToken, FileStatus.ErrorProcessing);
        }
    }

    private async Task<IValidatorV2?> GetValidatorAsync(ValidationJobFile file, IEnumerable<IValidatorV2> validators,
        CancellationToken stoppingToken)
    {
        var ext = Path.GetExtension(file.OriginalFileName);

        // TODO: Implement mandate specific validator selection logic
        var mandateConfig = GetValidationConfigForMandateAsync();
        if (mandateConfig != null)
        {
            // TODO: Logic
        }

        return await FindByExtensionAsync(ext, validators, stoppingToken);
    }
}
