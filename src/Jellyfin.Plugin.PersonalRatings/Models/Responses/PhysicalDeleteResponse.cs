namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Response model for batch physical deletion.
/// </summary>
public sealed class PhysicalDeleteResponse
{
    /// <summary>
    /// Gets or sets the batch operation name.
    /// </summary>
    public string Operation { get; set; } = "deletePhysical";

    /// <summary>
    /// Gets or sets the number of requested items.
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successfully deleted items.
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed items.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the per-item results.
    /// </summary>
    public IReadOnlyList<PhysicalDeleteItemResponse> Items { get; set; } = Array.Empty<PhysicalDeleteItemResponse>();
}
