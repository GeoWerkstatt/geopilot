﻿namespace Geopilot.Api.FileAccess;

/// <summary>
/// Provides access to the upload and asset directories.
/// </summary>
public interface IDirectoryProvider
{
    /// <summary>
    /// Gets the upload directory for the specified <paramref name="jobId"/>.
    /// </summary>
    /// <returns>The path of the upload directory.</returns>
    string GetUploadDirectoryPath(Guid jobId);

    /// <summary>
    /// Gets the asset directory for the specified <paramref name="jobId"/>.
    /// </summary>
    /// <returns>The path of the asset directory.</returns>
    string GetAssetDirectoryPath(Guid jobId);
}
