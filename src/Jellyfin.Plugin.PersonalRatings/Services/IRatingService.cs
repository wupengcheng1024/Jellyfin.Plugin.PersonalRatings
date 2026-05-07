using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;

namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Business service for personal ratings workflows.
/// </summary>
public interface IRatingService
{
    /// <summary>
    /// Gets the current user's rating for one item.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemId">The Jellyfin item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current rating response.</returns>
    Task<RatingResponse> GetRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the current user's rating for one item.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemId">The Jellyfin item id.</param>
    /// <param name="score">The rating score.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated rating response.</returns>
    Task<RatingResponse> SetRatingAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the current user's rating for one item.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemId">The Jellyfin item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cleared rating response.</returns>
    Task<RatingResponse> ClearRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Queries the current user's ratings with filters and paging.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paged query response.</returns>
    Task<RatingQueryResponse> QueryRatingsAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Applies one score to many items.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="score">The rating score.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    Task<BatchOperationResponse> BatchSetRatingAsync(Guid userId, IReadOnlyList<Guid> itemIds, int score, CancellationToken cancellationToken);

    /// <summary>
    /// Clears ratings for many items.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    Task<BatchOperationResponse> BatchClearRatingAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the pending-delete state for many items.
    /// </summary>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="itemIds">The target item ids.</param>
    /// <param name="isPendingDelete">The target pending-delete state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    Task<BatchOperationResponse> BatchSetPendingDeleteAsync(Guid userId, IReadOnlyList<Guid> itemIds, bool isPendingDelete, CancellationToken cancellationToken);
}
