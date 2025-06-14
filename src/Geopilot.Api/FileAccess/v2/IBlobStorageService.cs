using Azure;
using Azure.Storage.Blobs.Models;
using Geopilot.Api.Models;

namespace Geopilot.Api.FileAccess.V2;

/// <summary>
/// Provides Azure Blob Storage operations with integrated Microsoft Defender for Storage support.
/// Handles file uploads, downloads, existence checks, and virus scan status retrieval.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Generates a presigned URL that allows external clients to upload files directly to blob storage.
    /// The URL includes write and create permissions and is valid for the specified duration.
    /// </summary>
    /// <param name="blobPath">The path where the blob will be stored in the container.</param>
    /// <param name="expiry">How long the presigned URL should remain valid.</param>
    /// <param name="contentType">Optional content type for the blob (currently not enforced by SAS).</param>
    /// <returns>A presigned URL that can be used for direct uploads.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the storage account lacks permissions to generate SAS URIs.</exception>
    Task<string> GeneratePresignedUploadUrlAsync(string blobPath, TimeSpan? expiry, string? contentType = null);

    /// <summary>
    /// Checks whether a blob exists at the specified path in the storage container.
    /// </summary>
    /// <param name="blobPath">The path of the blob to check for existence.</param>
    /// <returns>True if the blob exists, false otherwise.</returns>
    Task<bool> BlobExistsAsync(string blobPath);

    /// <summary>
    /// Downloads a blob from storage and returns it as a stream.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    /// <param name="blobPath">The path of the blob to download.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to abort the download operation. Particularly important for large files
    /// where download time may be significant and responsive cancellation is required.
    /// </param>
    /// <returns>A stream containing the blob content.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified blob doesn't exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<Stream> DownloadBlobAsync(string blobPath, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the Microsoft Defender for Storage virus scan status for a blob.
    /// This method checks the blob's index tags for malware scanning results.
    /// </summary>
    /// <param name="blobPath">The path of the blob to check scan status for.</param>
    /// <returns>
    /// A <see cref="FileStatus"/> indicating the scan result:
    /// - <see cref="FileStatus.Clean"/> if no threats were found.
    /// - <see cref="FileStatus.Infected"/> if malware was detected.
    /// - <see cref="FileStatus.ErrorProcessing"/> if scanning failed or the blob doesn't exist.
    /// - <see cref="FileStatus.AwaitingVirusScanResult"/> if scanning is pending or the tag is missing/unrecognized.
    /// </returns>
    Task<FileStatus> GetVirusScanStatusAsync(string blobPath);

    /// <summary>
    /// Retrieves the properties (metadata) of a blob, including creation time, size, and other attributes.
    /// </summary>
    /// <param name="blobPath">The path of the blob to get properties for.</param>
    /// <returns>The blob properties if the blob exists; otherwise, null.</returns>
    Task<BlobProperties?> GetBlobPropertiesAsync(string blobPath);

    /// <summary>
    /// Uploads content from a stream to Azure Blob Storage at the specified blob path.
    /// </summary>
    /// <param name="blobName">
    /// The full path/name of the blob within the container where the content will be stored.
    /// This serves as the unique identifier for the blob and determines its virtual directory structure.
    /// For example: "validation-logs/job-123/file-456/Log_abc.log"
    /// </param>
    /// <param name="contentStream">
    /// The stream containing the content to upload. The stream will be read from its current position
    /// to the end. If the stream is seekable, it will be reset to position 0 before uploading.
    /// The stream is not disposed by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the upload operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous upload operation. The task completes when the
    /// blob has been successfully uploaded to Azure Storage.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="blobName"/> or <paramref name="contentStream"/> is null.
    /// </exception>
    /// <exception cref="RequestFailedException">
    /// Thrown when the Azure Storage service returns an error, such as insufficient permissions,
    /// storage account limits exceeded, or network connectivity issues.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    Task UploadBlobAsync(string blobName, Stream contentStream, CancellationToken cancellationToken = default);
}
