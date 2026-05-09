namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// A single delete audit-log row used by the management page.
/// </summary>
public sealed class AuditLogListItemResponse
{
    /// <summary>
    /// Gets or sets the database row id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the operator Jellyfin user id.
    /// </summary>
    public string OperatorUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detail message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
