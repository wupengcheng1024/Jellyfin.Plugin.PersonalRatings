using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class JellyfinDeletionAdapter : IJellyfinDeletionAdapter
{
    private readonly ILibraryManager _libraryManager;

    public JellyfinDeletionAdapter(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    public Task<JellyfinDeletionTarget?> GetTargetAsync(Guid itemId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        BaseItem? item = _libraryManager.GetItemById<BaseItem>(itemId);
        if (item is null)
        {
            return Task.FromResult<JellyfinDeletionTarget?>(null);
        }

        JellyfinDeletionTarget target = new()
        {
            ItemId = item.Id,
            ItemName = item.Name,
            ItemType = item.GetClientTypeName(),
            Item = item
        };

        return Task.FromResult<JellyfinDeletionTarget?>(target);
    }

    public Task DeleteAsync(JellyfinDeletionTarget target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DeleteOptions deleteOptions = new()
        {
            DeleteFileLocation = true,
            DeleteFromExternalProvider = false
        };

        _libraryManager.DeleteItem(target.Item, deleteOptions, notifyParentItem: true);
        return Task.CompletedTask;
    }
}
