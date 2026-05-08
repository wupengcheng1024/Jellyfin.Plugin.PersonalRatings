namespace Jellyfin.Plugin.PersonalRatings.Services;

internal interface IJellyfinDeletionAdapter
{
    Task<JellyfinDeletionTarget?> GetTargetAsync(Guid itemId, CancellationToken cancellationToken);

    Task DeleteAsync(JellyfinDeletionTarget target, CancellationToken cancellationToken);
}
