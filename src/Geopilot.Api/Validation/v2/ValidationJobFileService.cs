using Geopilot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Implementation of validation job file management service.
/// Handles database operations for ValidationJobFile entities with storage-agnostic path generation.
/// </summary>
public class ValidationJobFileService : IValidationJobFileService
{
    private readonly Context context;
    private readonly ILogger<ValidationJobFileService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationJobFileService"/> class.
    /// </summary>
    /// <param name="context">
    /// The Entity Framework database context used for ValidationJobFile and ValidationJob operations.
    /// Must be properly configured with the appropriate connection string and model mappings.
    /// </param>
    /// <param name="logger">
    /// Logger instance for tracking file creation operations, validation errors, and performance metrics.
    /// Used for debugging file entry creation issues and monitoring service usage.
    /// </param>
    public ValidationJobFileService(Context context, ILogger<ValidationJobFileService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ValidationJobFile>> CreateFileEntriesAsync(Guid jobId, IEnumerable<string> fileNames, StorageType storageType)
    {
        ArgumentNullException.ThrowIfNull(fileNames);

        var fileNamesList = fileNames.Distinct().ToList();
        if (!fileNamesList.Any())
        {
            throw new ArgumentException("File names collection cannot be empty", nameof(fileNames));
        }

        // Verify job exists
        var jobExists = await context.ValidationJobs.AnyAsync(j => j.Id == jobId);
        if (!jobExists)
        {
            throw new ArgumentException($"Validation job with ID {jobId} not found", nameof(jobId));
        }

        var files = new List<ValidationJobFile>();

        foreach (var fileName in fileNamesList)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                logger.LogWarning("Skipping empty or whitespace file name for job {JobId}", jobId);
                continue;
            }

            var sanitizedFileName = SanitizeFileName(fileName);
            var location = GenerateLocation(jobId, sanitizedFileName, storageType);

            var file = new ValidationJobFile
            {
                ValidationJobId = jobId,
                OriginalFileName = fileName,
                Location = location,
                StorageType = storageType,
                FileStatus = FileStatus.Pending
            };

            files.Add(file);
        }

        if (files.Any())
        {
            await context.ValidationJobFiles.AddRangeAsync(files);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {FileCount} file entries for job {JobId} with storage type {StorageType}",
                files.Count, jobId, storageType);
        }

        return files;
    }

    /// <summary>
    /// Generates storage location paths based on storage backend type and file information.
    /// </summary>
    /// <param name="jobId">The validation job ID used for path organization.</param>
    /// <param name="fileName">The sanitized file name to include in the path.</param>
    /// <param name="storageType">The storage backend type that determines path format.</param>
    /// <returns>A location path appropriate for the specified storage type.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when an unsupported storage type is provided.
    /// </exception>
    private static string GenerateLocation(Guid jobId, string fileName, StorageType storageType)
    {
        return storageType switch
        {
            StorageType.AzureBlobStorage => $"validation-jobs/{jobId}/{Guid.NewGuid():N}_{fileName}",
            StorageType.LocalFileSystem => $"/uploads/{jobId}/{fileName}",
            _ => throw new NotSupportedException($"Storage type {storageType} is not supported")
        };
    }

}
