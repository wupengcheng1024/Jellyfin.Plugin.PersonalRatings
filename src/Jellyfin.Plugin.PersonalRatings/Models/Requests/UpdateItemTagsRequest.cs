using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for replacing all tags on one item.
/// </summary>
public sealed class UpdateItemTagsRequest
{
    /// <summary>
    /// Gets or sets the target Jellyfin item id.
    /// </summary>
    [Required]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target tag ids.
    /// </summary>
    [Required]
    public List<long> TagIds { get; set; } = [];
}
