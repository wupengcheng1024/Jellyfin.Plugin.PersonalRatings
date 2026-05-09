namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Response model for a single item rating.
/// </summary>
public sealed class RatingResponse
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is pending deletion.
    /// </summary>
    public bool IsPendingDelete { get; set; }

    /// <summary>
    /// Gets or sets the last played timestamp.
    /// </summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is played.
    /// </summary>
    public bool IsPlayed { get; set; }

    /// <summary>
    /// Gets or sets the rating timestamp.
    /// </summary>
    public DateTimeOffset? RatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin media type.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item type.
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the assigned tags.
    /// </summary>
    public IReadOnlyList<TagReferenceResponse> Tags { get; set; } = Array.Empty<TagReferenceResponse>();
}
