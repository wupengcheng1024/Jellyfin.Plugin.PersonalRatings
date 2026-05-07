namespace Jellyfin.Plugin.PersonalRatings.Models.Entities;

internal sealed class JellyfinItemMetadata
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ClientTypeName { get; set; } = string.Empty;

    public string MediaType { get; set; } = string.Empty;

    public int? ProductionYear { get; set; }

    public DateTimeOffset? DateCreatedUtc { get; set; }
}
