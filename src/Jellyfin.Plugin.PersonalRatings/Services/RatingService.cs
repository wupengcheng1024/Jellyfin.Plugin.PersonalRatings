using Jellyfin.Plugin.PersonalRatings.Configuration;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class RatingService : IRatingService
{
    private const int MaxPageSize = 200;
    private readonly IJellyfinItemResolver _itemResolver;
    private readonly ILogger<RatingService> _logger;
    private readonly IRatingRepository _ratingRepository;

    public RatingService(
        IRatingRepository ratingRepository,
        IJellyfinItemResolver itemResolver,
        ILogger<RatingService> logger)
    {
        _ratingRepository = ratingRepository;
        _itemResolver = itemResolver;
        _logger = logger;
    }

    public async Task<RatingResponse> GetRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating? rating = await _ratingRepository.GetAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata);
    }

    public async Task<RatingResponse> SetRatingAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken)
    {
        if (score < 1 || score > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");
        }

        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating rating = await _ratingRepository.UpsertScoreAsync(userId, itemId, score, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata);
    }

    public async Task<RatingResponse> ClearRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating rating = await _ratingRepository.ClearScoreAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata);
    }

    public async Task<RatingQueryResponse> QueryRatingsAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken)
    {
        ValidateQueryRequest(request);

        if (request.LibraryIds.Count > 0)
        {
            _logger.LogWarning("LibraryIds filter was requested but is not applied in phase 1 because library ownership mapping still needs Jellyfin 10.10.7 verification.");
        }

        bool requiresMetadataFiltering = request.MediaTypes.Count > 0
            || request.Year.HasValue
            || !string.IsNullOrWhiteSpace(request.Keyword);

        if (!requiresMetadataFiltering)
        {
            Data.PagedQueryResult<UserItemRating> pagedRows = await _ratingRepository.QueryPageAsync(userId, request, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<RatingListItemResponse> mappedItems = await MapListItemsAsync(userId, pagedRows.Items, cancellationToken).ConfigureAwait(false);

            return new RatingQueryResponse
            {
                Items = mappedItems,
                TotalCount = pagedRows.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        IReadOnlyList<UserItemRating> allRows = await _ratingRepository.ListAsync(userId, request, cancellationToken).ConfigureAwait(false);
        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> combined = await ResolveMetadataAsync(userId, allRows, cancellationToken).ConfigureAwait(false);

        IEnumerable<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> filtered = combined.Where(tuple => MatchesMetadataFilters(tuple.Metadata, request));
        int totalCount = filtered.Count();

        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> paged = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        IReadOnlyList<RatingListItemResponse> items = paged
            .Select(tuple => MapListResponse(tuple.Rating, tuple.Metadata))
            .ToList();

        return new RatingQueryResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static RatingListItemResponse MapListResponse(UserItemRating rating, JellyfinItemMetadata? metadata)
    {
        return new RatingListItemResponse
        {
            ItemId = rating.ItemId.ToString("D"),
            Score = rating.Score,
            IsPendingDelete = rating.IsPendingDelete,
            LastPlayedAt = rating.LastPlayedAt,
            RatedAt = rating.RatedAt,
            UpdatedAt = rating.UpdatedAt,
            CreatedAt = rating.CreatedAt,
            ItemName = metadata?.Name,
            MediaType = metadata?.MediaType,
            ItemType = metadata?.ClientTypeName,
            ProductionYear = metadata?.ProductionYear
        };
    }

    private static RatingResponse MapSingleResponse(UserItemRating? rating, JellyfinItemMetadata metadata)
    {
        return new RatingResponse
        {
            ItemId = metadata.ItemId.ToString("D"),
            Score = rating?.Score ?? 0,
            IsPendingDelete = rating?.IsPendingDelete ?? false,
            LastPlayedAt = rating?.LastPlayedAt,
            RatedAt = rating?.RatedAt,
            UpdatedAt = rating?.UpdatedAt,
            CreatedAt = rating?.CreatedAt,
            ItemName = metadata.Name,
            MediaType = metadata.MediaType,
            ItemType = metadata.ClientTypeName,
            ProductionYear = metadata.ProductionYear
        };
    }

    private static bool MatchesMetadataFilters(JellyfinItemMetadata? metadata, RatingQueryRequest request)
    {
        if (request.MediaTypes.Count > 0)
        {
            if (metadata is null)
            {
                return false;
            }

            bool mediaTypeMatch = request.MediaTypes.Any(type =>
                string.Equals(type, metadata.MediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, metadata.ClientTypeName, StringComparison.OrdinalIgnoreCase));

            if (!mediaTypeMatch)
            {
                return false;
            }
        }

        if (request.Year.HasValue && metadata?.ProductionYear != request.Year.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            if (metadata is null || string.IsNullOrWhiteSpace(metadata.Name))
            {
                return false;
            }

            if (metadata.Name.IndexOf(request.Keyword, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<IReadOnlyList<RatingListItemResponse>> MapListItemsAsync(
        Guid userId,
        IReadOnlyList<UserItemRating> ratings,
        CancellationToken cancellationToken)
    {
        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> combined = await ResolveMetadataAsync(userId, ratings, cancellationToken).ConfigureAwait(false);
        return combined.Select(tuple => MapListResponse(tuple.Rating, tuple.Metadata)).ToList();
    }

    private async Task<JellyfinItemMetadata> RequireMetadataAsync(Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata? metadata = await _itemResolver.GetMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            throw new ItemNotFoundException(itemId);
        }

        return metadata;
    }

    private async Task<List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)>> ResolveMetadataAsync(
        Guid userId,
        IReadOnlyList<UserItemRating> ratings,
        CancellationToken cancellationToken)
    {
        List<Task<JellyfinItemMetadata?>> metadataTasks = ratings
            .Select(rating => _itemResolver.GetMetadataAsync(rating.ItemId, userId, cancellationToken))
            .ToList();

        JellyfinItemMetadata?[] metadata = await Task.WhenAll(metadataTasks).ConfigureAwait(false);

        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> combined = [];
        for (int index = 0; index < ratings.Count; index++)
        {
            combined.Add((ratings[index], metadata[index]));
        }

        return combined;
    }

    private void ValidateQueryRequest(RatingQueryRequest request)
    {
        if (request.Score.HasValue && (request.Score.Value < 0 || request.Score.Value > 5))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Score), "Score filter must be between 0 and 5.");
        }

        request.PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;

        int configuredDefaultPageSize = Plugin.Instance?.Configuration.DefaultPageSize ?? PluginConfiguration.DefaultPageSizeValue;
        int defaultPageSize = configuredDefaultPageSize <= 0 ? PluginConfiguration.DefaultPageSizeValue : configuredDefaultPageSize;
        request.PageSize = request.PageSize <= 0 ? defaultPageSize : Math.Min(request.PageSize, MaxPageSize);
    }
}
