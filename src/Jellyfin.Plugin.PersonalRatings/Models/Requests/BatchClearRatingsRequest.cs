using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for batch clear operations.
/// </summary>
public sealed class BatchClearRatingsRequest
{
    /// <summary>
    /// Gets or sets the target Jellyfin item ids.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> ItemIds { get; set; } = [];
}
