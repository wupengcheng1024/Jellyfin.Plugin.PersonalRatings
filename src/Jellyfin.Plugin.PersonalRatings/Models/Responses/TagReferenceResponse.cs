namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Lightweight tag payload used by ratings and item-tag responses.
/// </summary>
public sealed class TagReferenceResponse
{
    /// <summary>
    /// Gets or sets the tag id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tag color.
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }
}
