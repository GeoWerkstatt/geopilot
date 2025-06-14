namespace Geopilot.Api.FileAccess.V2;

/// <summary>
/// Configuration options for Azure Blob Storage service connection and behavior.
/// Used to configure the <see cref="AzureBlobStorageService"/> with connection details and default settings.
/// </summary>
public class AzureBlobStorageOptions
{
    /// <summary>
    /// Gets or sets the Azure Storage account connection string.
    /// This should include the account name, key, and endpoints required to connect to the storage service.
    /// </summary>
    /// <value>
    /// A valid Azure Storage connection string. Defaults to an empty string if not configured.
    /// </value>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the blob container where files will be stored.
    /// The container will be created automatically if it doesn't exist.
    /// </summary>
    /// <value>
    /// A valid Azure Blob Storage container name. Defaults to an empty string if not configured.
    /// </value>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default expiry time in minutes for presigned upload URLs.
    /// This determines how long generated SAS URIs remain valid for file uploads.
    /// </summary>
    /// <value>
    /// The number of minutes a presigned URL should remain valid. Defaults to 15 minutes.
    /// </value>
    public int PresignedUrlExpiryMinutes { get; set; } = 15;
}
