using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for ratings queries.
/// </summary>
public sealed class RatingQueryRequest
{
    /// <summary>
    /// Gets or sets the exact score filter.
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// Gets or sets the played-state filter.
    /// </summary>
    public bool? IsPlayed { get; set; }

    /// <summary>
    /// Gets or sets the pending-delete filter.
    /// </summary>
    public bool? IsPendingDelete { get; set; }

    /// <summary>
    /// Gets or sets the library filters.
    /// </summary>
    public List<string> LibraryIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the media type filters.
    /// </summary>
    public List<string> MediaTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the production year filter.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the keyword filter.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortBy { get; set; } = "updatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(0, 500)]
    public int PageSize { get; set; }
}
