namespace Jellyfin.Plugin.PersonalRatings.Models.Entities;

internal sealed class UserItemTag
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public long TagId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
