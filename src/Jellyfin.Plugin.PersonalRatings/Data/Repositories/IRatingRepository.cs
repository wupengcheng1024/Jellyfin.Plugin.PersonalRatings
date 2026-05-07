using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal interface IRatingRepository
{
    Task<UserItemRating?> GetAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    Task<UserItemRating> UpsertScoreAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserItemRating>> UpsertScoresAsync(Guid userId, IReadOnlyList<Guid> itemIds, int score, CancellationToken cancellationToken);

    Task<UserItemRating> ClearScoreAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserItemRating>> ClearScoresAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserItemRating>> SetPendingDeleteAsync(Guid userId, IReadOnlyList<Guid> itemIds, bool isPendingDelete, CancellationToken cancellationToken);

    Task<PagedQueryResult<UserItemRating>> QueryPageAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserItemRating>> ListAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken);
}
