using Geopilot.Api.Models;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Defines the service contract for managing validation job file entries in the database.
/// This service handles essential CRUD operations on ValidationJobFile entities and is storage-backend agnostic.
/// </summary>
public interface IValidationJobFileService
{
    /// <summary>
    /// Creates new file entries in the database for a validation job.
    /// Generates appropriate storage locations based on the storage type.
    /// </summary>
    /// <param name="jobId">The validation job ID to associate the files with.</param>
    /// <param name="fileNames">Collection of file names to create entries for.</param>
    /// <param name="storageType">The storage backend type where files will be stored.</param>
    /// <returns>A list of created ValidationJobFile entities with generated IDs and locations.</returns>
    Task<List<ValidationJobFile>> CreateFileEntriesAsync(Guid jobId, IEnumerable<string> fileNames, StorageType storageType);
}
