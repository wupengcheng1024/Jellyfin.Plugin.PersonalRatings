namespace Jellyfin.Plugin.PersonalRatings.Data;

internal sealed class PagedQueryResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }
}
