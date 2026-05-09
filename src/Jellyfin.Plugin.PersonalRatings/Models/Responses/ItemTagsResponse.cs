namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Response model for one item's assigned tags.
/// </summary>
public sealed class ItemTagsResponse
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assigned tags.
    /// </summary>
    public IReadOnlyList<TagReferenceResponse> Tags { get; set; } = Array.Empty<TagReferenceResponse>();
}
