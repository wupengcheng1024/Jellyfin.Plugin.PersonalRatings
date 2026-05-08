using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for batch physical deletion.
/// </summary>
public sealed class BatchPhysicalDeleteRequest
{
    /// <summary>
    /// Gets or sets the target Jellyfin item ids.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> ItemIds { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the caller has explicitly confirmed the physical delete action.
    /// </summary>
    public bool ConfirmDelete { get; set; }
}
