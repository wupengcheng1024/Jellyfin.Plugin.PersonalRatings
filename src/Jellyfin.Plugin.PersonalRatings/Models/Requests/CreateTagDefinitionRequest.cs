using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.PersonalRatings.Models.Requests;

/// <summary>
/// Request model for creating a tag definition.
/// </summary>
public sealed class CreateTagDefinitionRequest
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [Required]
    [MaxLength(48)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tag color.
    /// </summary>
    [MaxLength(32)]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tag is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
