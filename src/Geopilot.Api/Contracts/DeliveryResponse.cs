namespace Geopilot.Api.Contracts;

/// <summary>
/// Represents the data sent to a client after a successful delivery creation.
/// This contract provides a comprehensive summary of the created delivery.
/// </summary>
public class DeliveryResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the delivery.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the validation job that this delivery is based on.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the delivery was created.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets a summary of the user who declared the delivery.
    /// </summary>
    /// <seealso cref="UserSummary"/>
    public UserSummary DeclaringUser { get; set; } = new();

    /// <summary>
    /// Gets or sets a summary of the mandate under which the delivery was made.
    /// </summary>
    /// <seealso cref="MandateSummary"/>
    public MandateSummary Mandate { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of all assets included in this delivery.
    /// </summary>
    /// <seealso cref="AssetSummary"/>
    public List<AssetSummary> Assets { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this was a partial delivery.
    /// <c>null</c> if this information was not provided or not applicable.
    /// </summary>
    public bool? Partial { get; set; }

    /// <summary>
    /// Gets or sets the optional comment provided by the user for this delivery.
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Provides a minimal summary of a user.
/// </summary>
public class UserSummary
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// Provides a minimal summary of a mandate.
/// </summary>
public class MandateSummary
{
    /// <summary>
    /// Gets or sets the unique identifier of the mandate.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the descriptive name of the mandate.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Provides a summary of a single asset (file) within a delivery.
/// </summary>
public class AssetSummary
{
    /// <summary>
    /// Gets or sets the unique identifier of the asset record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the original filename of the asset as uploaded by the user.
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the asset, represented as a string.
    /// </summary>
    /// <remarks>
    /// This is the string representation of the <c>AssetType</c> enum.
    /// </remarks>
    /// <example>PrimaryData, ValidationReport</example>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA-256 hash of the file content, encoded as a Base64 string.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;
}
