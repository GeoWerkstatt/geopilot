namespace Geopilot.Api.Contracts;

/// <summary>
/// Response returned after the <c>/presign</c> call,
/// containing the validation job identifier and upload URLs.
/// </summary>
public sealed class UploadUrlsResponse
{
    /// <summary/>
    public Guid ValidationJobId { get; set; }

    /// <summary>
    /// One SAS URL per requested file.
    /// </summary>
    public List<UploadUrl> UploadUrls { get; set; } = new();
}

/// <summary>
/// Describes a single pre-signed (SAS) URL the client can use to upload a blob.
/// </summary>
public sealed class UploadUrl
{
    /// <summary>
    /// Friendly file name as provided by the client (used for display only).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The full HTTPS URL (incl. SAS token) that accepts an HTTP PUT from the client.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// How long the URL remains usable until Azure rejects the request.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
