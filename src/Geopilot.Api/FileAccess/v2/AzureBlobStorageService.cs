using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Geopilot.Api.Models;
using Microsoft.Extensions.Options;

namespace Geopilot.Api.FileAccess.V2;

/// <inheritdoc />
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient blobContainerClient;
    private readonly AzureBlobStorageOptions azureBlobStorageOptions;
    private readonly ILogger<AzureBlobStorageService> logger;

    /// <summary>
    /// The standard tag name used by Microsoft Defender for Storage to store malware scan results.
    /// </summary>
    private const string DefenderScanResultTag = "Malware Scanning scan result";

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
    /// Creates the blob container if it doesn't exist and configures the underlying Azure SDK clients.
    /// </summary>
    /// <param name="azureBlobStorageOptions">
    /// Configuration options containing the connection string and container name.
    /// </param>
    /// <param name="logger">
    /// Logger for tracking operations and errors.
    /// </param>
    /// <param name="blobClientOptions">
    /// Optional Azure SDK client configuration for customizing HTTP behavior, timeouts, and retry policies.
    /// If null, default Azure SDK settings are used.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Azure Storage connection string is missing or invalid.
    /// </exception>
    /// <exception cref="RequestFailedException">
    /// Thrown if the service fails to connect to Azure Storage or create the container,
    /// often due to invalid credentials or insufficient permissions.
    /// </exception>
    public AzureBlobStorageService(
        IOptions<AzureBlobStorageOptions> azureBlobStorageOptions,
        ILogger<AzureBlobStorageService> logger,
        BlobClientOptions? blobClientOptions = null  // Add this optional parameter
    )
    {
        this.logger = logger;
        this.azureBlobStorageOptions = azureBlobStorageOptions.Value;

        // Pass the options to BlobServiceClient, container client inherits them
        var blobServiceClient = blobClientOptions != null
            ? new BlobServiceClient(this.azureBlobStorageOptions.ConnectionString, blobClientOptions)
            : new BlobServiceClient(this.azureBlobStorageOptions.ConnectionString);

        blobContainerClient = blobServiceClient.GetBlobContainerClient(this.azureBlobStorageOptions.ContainerName);
        blobContainerClient.CreateIfNotExists();
    }

