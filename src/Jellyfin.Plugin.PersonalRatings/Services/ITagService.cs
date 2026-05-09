using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;

namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Business service for tag definitions and user item-tag assignments.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Lists tag definitions.
    /// </summary>
    /// <param name="operatorUserId">The current Jellyfin user id.</param>
    /// <param name="includeDisabled">Whether disabled tags should be included.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tag definitions.</returns>
    Task<IReadOnlyList<TagDefinitionResponse>> GetTagDefinitionsAsync(
        Guid operatorUserId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates one tag definition.
    /// </summary>
    /// <param name="operatorUserId">The current Jellyfin user id.</param>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created tag definition.</returns>
    Task<TagDefinitionResponse> CreateTagDefinitionAsync(
        Guid operatorUserId,
        CreateTagDefinitionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates one tag definition.
    /// </summary>
    /// <param name="operatorUserId">The current Jellyfin user id.</param>
    /// <param name="tagId">The target tag id.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tag definition.</returns>
    Task<TagDefinitionResponse> UpdateTagDefinitionAsync(
        Guid operatorUserId,
        long tagId,
        UpdateTagDefinitionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes one tag definition.
    /// </summary>
    /// <param name="operatorUserId">The current Jellyfin user id.</param>
    /// <param name="tagId">The target tag id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteTagDefinitionAsync(Guid operatorUserId, long tagId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current user's tags for one item.
    /// </summary>
    /// <param name="userId">The current Jellyfin user id.</param>
    /// <param name="itemId">The target item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assigned item tags.</returns>
    Task<ItemTagsResponse> GetItemTagsAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces all tags for one item.
    /// </summary>
    /// <param name="userId">The current Jellyfin user id.</param>
    /// <param name="itemId">The target item id.</param>
    /// <param name="tagIds">The target tag ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated item tags.</returns>
    Task<ItemTagsResponse> ReplaceItemTagsAsync(Guid userId, Guid itemId, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);

    /// <summary>
    /// Adds tags to many items.
    /// </summary>
    /// <param name="userId">The current Jellyfin user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="tagIds">The tag ids to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    Task<BatchOperationResponse> BatchAddTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);

    /// <summary>
    /// Removes tags from many items.
    /// </summary>
    /// <param name="userId">The current Jellyfin user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="tagIds">The tag ids to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    Task<BatchOperationResponse> BatchRemoveTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);
}
