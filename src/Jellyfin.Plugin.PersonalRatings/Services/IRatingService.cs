using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;

namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Business service for personal ratings workflows.
/// </summary>
public interface IRatingService
{
    Task<RatingResponse> GetRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    Task<RatingResponse> SetRatingAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken);

    Task<RatingResponse> ClearRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    Task<RatingQueryResponse> QueryRatingsAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken);
}
