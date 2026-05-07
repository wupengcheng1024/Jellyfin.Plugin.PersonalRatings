namespace Jellyfin.Plugin.PersonalRatings.Models.Entities;

internal sealed class DeleteAuditLog
{
    public long Id { get; set; }

    public Guid OperatorUserId { get; set; }

    public Guid ItemId { get; set; }

    public string? ItemName { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
