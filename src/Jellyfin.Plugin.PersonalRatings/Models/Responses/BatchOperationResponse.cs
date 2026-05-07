namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Response model for batch rating operations.
/// </summary>
public sealed class BatchOperationResponse
{
    /// <summary>
    /// Gets or sets the batch operation name.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of requested items.
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of affected items.
    /// </summary>
    public int AffectedCount { get; set; }

    /// <summary>
    /// Gets or sets the updated items.
    /// </summary>
    public IReadOnlyList<RatingResponse> Items { get; set; } = Array.Empty<RatingResponse>();
}
