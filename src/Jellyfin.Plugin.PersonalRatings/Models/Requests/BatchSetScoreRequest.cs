using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for batch score updates.
/// </summary>
public sealed class BatchSetScoreRequest
{
    /// <summary>
    /// Gets or sets the target Jellyfin item ids.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> ItemIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the score to apply.
    /// </summary>
    [Range(1, 5)]
    public int Score { get; set; }
}
