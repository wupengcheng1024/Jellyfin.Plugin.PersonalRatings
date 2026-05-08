using Jellyfin.Plugin.PersonalRatings.Models.Responses;

namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Business service for administrator-only physical deletion workflows.
/// </summary>
public interface IDeletionService
{
    /// <summary>
    /// Physically deletes the requested Jellyfin items and records audit logs for every attempted item.
    /// </summary>
    /// <param name="operatorUserId">The Jellyfin operator user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="confirmDelete">Whether the caller explicitly confirmed physical deletion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The physical delete response.</returns>
    Task<PhysicalDeleteResponse> DeleteItemsAsync(Guid operatorUserId, IReadOnlyList<Guid> itemIds, bool confirmDelete, CancellationToken cancellationToken);
}
