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
}
