namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Per-item result for batch physical deletion.
/// </summary>
public sealed class PhysicalDeleteItemResponse
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin item name.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the deletion result code.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable message for the result.
    /// </summary>
    public string? Message { get; set; }
}
