using Jellyfin.Plugin.PersonalRatings.Models.Entities;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal interface IJellyfinItemResolver
{
    Task<JellyfinItemMetadata?> GetMetadataAsync(Guid itemId, Guid userId, CancellationToken cancellationToken);
}
