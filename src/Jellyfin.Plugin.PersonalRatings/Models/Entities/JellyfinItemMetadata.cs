namespace Jellyfin.Plugin.PersonalRatings.Models.Entities;

internal sealed class JellyfinItemMetadata
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ClientTypeName { get; set; } = string.Empty;

    public string MediaType { get; set; } = string.Empty;

    public int? ProductionYear { get; set; }

    public DateTimeOffset? DateCreatedUtc { get; set; }

    public IReadOnlyList<Guid> LibraryIds { get; set; } = Array.Empty<Guid>();

    public IReadOnlyList<string> LibraryNames { get; set; } = Array.Empty<string>();

    public bool IsPlayed { get; set; }

    public DateTimeOffset? LastPlayedAt { get; set; }
}
