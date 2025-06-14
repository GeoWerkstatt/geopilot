using System.ComponentModel.DataAnnotations;

namespace Geopilot.Api.Contracts;

/// <summary>
/// Request payload sent by a client to obtain one pre-signed upload URL
/// for every file it plans to transfer to blob storage.
/// </summary>
public sealed class UploadUrlsRequest
{
    /// <summary>
    /// Logical file names (including extension) that the client will later upload.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> FileNames { get; set; } = new();
}
