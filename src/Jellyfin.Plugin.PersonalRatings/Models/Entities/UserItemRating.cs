namespace Jellyfin.Plugin.PersonalRatings.Models.Entities;

internal sealed class UserItemRating
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public int Score { get; set; }

    public bool IsPendingDelete { get; set; }

    public DateTimeOffset? LastPlayedAt { get; set; }

    public DateTimeOffset? RatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
