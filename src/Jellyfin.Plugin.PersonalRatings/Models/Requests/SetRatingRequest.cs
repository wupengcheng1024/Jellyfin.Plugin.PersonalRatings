using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for setting a rating.
/// </summary>
public sealed class SetRatingRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    [Required]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating score.
    /// </summary>
    [Range(1, 5)]
    public int Score { get; set; }
}
