namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Paginated query response for the ratings management page.
/// </summary>
public sealed class RatingQueryResponse
{
    /// <summary>
    /// Gets or sets the result items.
    /// </summary>
    public IReadOnlyList<RatingListItemResponse> Items { get; set; } = Array.Empty<RatingListItemResponse>();

    /// <summary>
    /// Gets or sets the total number of matches.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the current page size.
    /// </summary>
    public int PageSize { get; set; }
}
