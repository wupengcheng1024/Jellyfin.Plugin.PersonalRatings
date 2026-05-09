using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for batch tag updates.
/// </summary>
public sealed class BatchTagRequest
{
    /// <summary>
    /// Gets or sets the target Jellyfin item ids.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> ItemIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the target tag ids.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<long> TagIds { get; set; } = [];
}
