using Jellyfin.Data.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class JellyfinItemResolver : IJellyfinItemResolver
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<JellyfinItemResolver> _logger;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;

    public JellyfinItemResolver(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILogger<JellyfinItemResolver> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    public Task<JellyfinItemMetadata?> GetMetadataAsync(Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        User? user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Task.FromResult<JellyfinItemMetadata?>(null);
        }

        BaseItem? item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return Task.FromResult<JellyfinItemMetadata?>(null);
        }

        DateTimeOffset? dateCreatedUtc = item.DateCreated == DateTime.MinValue
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(item.DateCreated, DateTimeKind.Utc));

        UserItemData userData = _userDataManager.GetUserData(user, item);
        DateTimeOffset? lastPlayedAt = userData.LastPlayedDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(userData.LastPlayedDate.Value, DateTimeKind.Utc))
            : null;

        IReadOnlyList<Folder> collectionFolders = _libraryManager.GetCollectionFolders(item);
        List<Guid> libraryIds = [];
        List<string> libraryNames = [];

        foreach (Folder folder in collectionFolders)
        {
            AddLibrary(folder.Id, folder.Name, libraryIds, libraryNames);
        }

        BaseItem? topParent = item.GetTopParent();
        if (topParent is not null)
        {
            AddLibrary(topParent.Id, topParent.Name, libraryIds, libraryNames);
        }

        JellyfinItemMetadata metadata = new()
        {
            ItemId = item.Id,
            Name = item.Name,
            ClientTypeName = item.GetClientTypeName(),
            MediaType = item.MediaType.ToString(),
            ProductionYear = item.ProductionYear,
            DateCreatedUtc = dateCreatedUtc,
            LibraryIds = libraryIds,
            LibraryNames = libraryNames,
            IsPlayed = item.IsPlayed(user),
            LastPlayedAt = lastPlayedAt
        };

        return Task.FromResult<JellyfinItemMetadata?>(metadata);
    }

    public async Task<IReadOnlyList<JellyfinItemMetadata>> GetMetadataAsync(IReadOnlyList<Guid> itemIds, Guid userId, CancellationToken cancellationToken)
    {
        List<JellyfinItemMetadata> items = [];
        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            JellyfinItemMetadata? metadata = await GetMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
            if (metadata is null)
            {
                _logger.LogWarning("Skipped item {ItemId} because it was not found or not accessible for user {UserId}", itemId, userId);
                continue;
            }

            items.Add(metadata);
        }

        return items;
    }

    private static void AddLibrary(Guid id, string? name, List<Guid> libraryIds, List<string> libraryNames)
    {
        if (!libraryIds.Contains(id))
        {
            libraryIds.Add(id);
        }

        if (!string.IsNullOrWhiteSpace(name) && !libraryNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            libraryNames.Add(name);
        }
    }
}
