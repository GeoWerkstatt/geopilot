using Geopilot.Api.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Implementation of validation job lifecycle management.
/// Handles pure job operations without file or storage concerns.
/// </summary>
public class ValidationJobService : IValidationJobService
{
    private readonly Context context;
    private readonly ILogger<ValidationJobService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationJobService"/> class.
    /// </summary>
    /// <param name="context">The Entity Framework database context for accessing validation-related entities.</param>
    /// <param name="logger">Logger instance for tracking job lifecycle operations and errors.</param>

    public ValidationJobService(Context context, ILogger<ValidationJobService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<Models.ValidationJob> CreateValidationJobAsync()
    {
        var validationJob = new Models.ValidationJob
        {
            Id = Guid.NewGuid(),
            Status = Models.ValidationJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.ValidationJobs.Add(validationJob);
        await context.SaveChangesAsync();

        logger.LogInformation("Created ValidationJob {JobId}", validationJob.Id);
        return validationJob;
    }

    /// <inheritdoc />
    public async Task<ValidationResponse?> StartValidationAsync(Guid jobId, ValidationRequest request)
    {
        var validationJob = await context.ValidationJobs
            .Include(job => job.Files)
            .FirstOrDefaultAsync(job => job.Id == jobId);

        if (validationJob == null)
        {
            return null;
        }

        if (validationJob.Status != Models.ValidationJobStatus.Pending)
        {
            throw new InvalidOperationException($"Job {jobId} cannot be started from status {validationJob.Status}");
        }

        if (request.MandateId <= 0)
        {
            throw new ArgumentException("MandateId is required to start validation");
        }

        if (!await context.Mandates.AnyAsync(m => m.Id == request.MandateId))
        {
            throw new ArgumentException($"Mandate with ID {request.MandateId} not found");
        }

        if (!validationJob.Files.Any())
        {
            throw new InvalidOperationException($"Job {jobId} has no files to validate");
        }

        validationJob.MandateId = request.MandateId;
        validationJob.Status = Models.ValidationJobStatus.Queued;

        await context.SaveChangesAsync();
        logger.LogInformation("ValidationJob {JobId} queued for processing with mandate {MandateId}", jobId, request.MandateId);

        return new ValidationResponse
        {
            ValidationJobId = jobId,
            ValidationJobStatus = validationJob.Status
        };
    }

    /// <inheritdoc />
    public async Task<ValidationStatusResponse?> GetJobStatusAsync(Guid jobId)
    {
        var validationJob = await context.ValidationJobs
            .AsNoTracking()
            .Include(job => job.Files)
            .ThenInclude(file => file.Logs)
            .FirstOrDefaultAsync(job => job.Id == jobId);

        if (validationJob == null)
        {
            return null;
        }

        return new ValidationStatusResponse
        {
            ValidationJobId = validationJob.Id,
            ValidationJobStatus = validationJob.Status,
            MandateId = validationJob.MandateId,
            Errors = validationJob.FailureReason,
            FileReports = validationJob.Files.Select(file => new FileReport
            {
                FileName = file.OriginalFileName,
                FileStatus = file.FileStatus,
                ValidationResult = file.ValidationResult,
                UploadedAt = file.UploadedAt,
                FileSizeBytes = file.FileSizeBytes,
                Logs = file.Logs.Select(log => new ValidationLog
                {
                    Id = log.Id,
                    LogName = log.LogName
                }).ToList()
            }).ToList()
        };
    }
}
