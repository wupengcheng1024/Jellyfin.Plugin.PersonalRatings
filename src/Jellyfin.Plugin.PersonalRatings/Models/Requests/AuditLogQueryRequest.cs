using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for delete audit-log queries.
/// </summary>
public sealed class AuditLogQueryRequest
{
    /// <summary>
    /// Gets or sets the minimum creation timestamp filter in UTC.
    /// </summary>
    public DateTimeOffset? CreatedAfterUtc { get; set; }

    /// <summary>
    /// Gets or sets the maximum creation timestamp filter in UTC.
    /// </summary>
    public DateTimeOffset? CreatedBeforeUtc { get; set; }

    /// <summary>
    /// Gets or sets the result filter.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Gets or sets the keyword filter.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Gets or sets the item id filter.
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 200)]
    public int PageSize { get; set; } = 25;
}
