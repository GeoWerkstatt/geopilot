using Microsoft.EntityFrameworkCore;
using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Service for retrieving validation log files from various storage backends.
/// </summary>
public class ValidationJobLogService : IValidationJobLogService
{
    private readonly Context context;
    private readonly IBlobStorageService blobStorageService;
    private readonly ILogger<ValidationJobLogService> logger;

    /// <summary>
    /// Initializes a new instance of the ValidationJobLogService.
    /// </summary>
    /// <param name="context">Database context for accessing validation log metadata.</param>
    /// <param name="blobStorageService">Service for Azure Blob Storage operations.</param>
    /// <param name="logger">Logger for tracking operations and errors.</param>
    public ValidationJobLogService(
        Context context,
        IBlobStorageService blobStorageService,
        ILogger<ValidationJobLogService> logger)
    {
        this.context = context;
        this.blobStorageService = blobStorageService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<Stream?> GetLogContentAsync(Guid logId)
    {
        var log = await context.ValidationJobLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            logger.LogWarning("Validation log {LogId} not found", logId);
            return null;
        }

        try
        {
            return log.StorageType switch
            {
                StorageType.AzureBlobStorage => await blobStorageService.DownloadBlobAsync(log.Location, CancellationToken.None),
                // Add other storage types here as needed:
                // StorageType.LocalFileSystem => await fileSystemService.GetFileStreamAsync(log.Location),
                _ => throw new NotSupportedException($"Storage type {log.StorageType} is not supported for log retrieval")
            };
        }
        catch (FileNotFoundException)
        {
            logger.LogError("Log file not found in storage: {Location} (LogId: {LogId})", log.Location, logId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving log content from {StorageType} storage: {Location} (LogId: {LogId})",
                log.StorageType, log.Location, logId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ValidationJobLog?> GetLogInfoAsync(Guid logId)
    {
        return await context.ValidationJobLogs
            .AsNoTracking()
            .Include(l => l.ValidationJobFile)
            .FirstOrDefaultAsync(l => l.Id == logId);
    }
}
