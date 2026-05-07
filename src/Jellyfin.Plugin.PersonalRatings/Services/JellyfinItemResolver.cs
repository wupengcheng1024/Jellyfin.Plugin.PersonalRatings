using Jellyfin.Data.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class JellyfinItemResolver : IJellyfinItemResolver
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;

    public JellyfinItemResolver(ILibraryManager libraryManager, IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
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

        JellyfinItemMetadata metadata = new()
        {
            ItemId = item.Id,
            Name = item.Name,
            ClientTypeName = item.GetClientTypeName(),
            MediaType = item.MediaType.ToString(),
            ProductionYear = item.ProductionYear,
            DateCreatedUtc = dateCreatedUtc
        };

        return Task.FromResult<JellyfinItemMetadata?>(metadata);
    }
}
