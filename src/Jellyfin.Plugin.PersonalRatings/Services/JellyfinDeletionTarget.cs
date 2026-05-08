using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class JellyfinDeletionTarget
{
    public Guid ItemId { get; set; }

    public string? ItemName { get; set; }

    public string? ItemType { get; set; }

    public BaseItem Item { get; set; } = null!;
}
