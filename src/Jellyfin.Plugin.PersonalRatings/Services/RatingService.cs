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
    private readonly ITagRepository _tagRepository;

    public RatingService(
        IRatingRepository ratingRepository,
        ITagRepository tagRepository,
        IJellyfinItemResolver itemResolver,
        ILogger<RatingService> logger)
    {
        _ratingRepository = ratingRepository;
        _tagRepository = tagRepository;
        _itemResolver = itemResolver;
        _logger = logger;
    }

    public async Task<RatingResponse> GetRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating? rating = await _ratingRepository.GetAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetItemTagsAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata, tags);
    }

    public async Task<RatingResponse> SetRatingAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken)
    {
        if (score < 1 || score > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");
        }

        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating rating = await _ratingRepository.UpsertScoreAsync(userId, itemId, score, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetItemTagsAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata, tags);
    }

    public async Task<RatingResponse> ClearRatingAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        UserItemRating rating = await _ratingRepository.ClearScoreAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetItemTagsAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return MapSingleResponse(rating, metadata, tags);
    }

    public async Task<RatingQueryResponse> QueryRatingsAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken)
    {
        ValidateQueryRequest(request);

        bool requiresMetadataFiltering = request.IsPlayed.HasValue
            || request.LibraryIds.Count > 0
            || request.MediaTypes.Count > 0
            || request.Year.HasValue
            || request.AddedAfterUtc.HasValue
            || request.AddedBeforeUtc.HasValue
            || !string.IsNullOrWhiteSpace(request.Keyword);
        bool requiresMetadataSorting = RequiresMetadataSorting(request.SortBy);

        if (!requiresMetadataFiltering && !requiresMetadataSorting)
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

        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> filtered = combined
            .Where(tuple => MatchesCombinedFilters(tuple.Rating, tuple.Metadata, request))
            .ToList();

        int totalCount = filtered.Count;

        IEnumerable<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> ordered = SortCombined(filtered, request.SortBy, request.SortOrder);

        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> paged = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>> tagMap = await _tagRepository
            .GetItemTagsMapAsync(userId, paged.Select(tuple => tuple.Rating.ItemId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RatingListItemResponse> items = paged
            .Select(tuple => MapListResponse(
                tuple.Rating,
                tuple.Metadata,
                tagMap.TryGetValue(tuple.Rating.ItemId, out IReadOnlyList<TagDefinition>? tags)
                    ? tags
                    : Array.Empty<TagDefinition>()))
            .ToList();

        return new RatingQueryResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<BatchOperationResponse> BatchSetRatingAsync(Guid userId, IReadOnlyList<Guid> itemIds, int score, CancellationToken cancellationToken)
    {
        if (score < 1 || score > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");
        }

        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        IReadOnlyList<JellyfinItemMetadata> metadata = await RequireMetadataAsync(normalizedItemIds, userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UserItemRating> ratings = await _ratingRepository.UpsertScoresAsync(userId, normalizedItemIds, score, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Applied score {Score} to {Count} items for user {UserId}", score, ratings.Count, userId);
        return await BuildBatchResponseAsync("setScore", userId, normalizedItemIds.Count, ratings, metadata, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BatchOperationResponse> BatchClearRatingAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        IReadOnlyList<JellyfinItemMetadata> metadata = await RequireMetadataAsync(normalizedItemIds, userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UserItemRating> ratings = await _ratingRepository.ClearScoresAsync(userId, normalizedItemIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cleared scores for {Count} items for user {UserId}", ratings.Count, userId);
        return await BuildBatchResponseAsync("clearScore", userId, normalizedItemIds.Count, ratings, metadata, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BatchOperationResponse> BatchSetPendingDeleteAsync(Guid userId, IReadOnlyList<Guid> itemIds, bool isPendingDelete, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        IReadOnlyList<JellyfinItemMetadata> metadata = await RequireMetadataAsync(normalizedItemIds, userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UserItemRating> ratings = await _ratingRepository.SetPendingDeleteAsync(userId, normalizedItemIds, isPendingDelete, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Set pending delete state {PendingDelete} for {Count} items for user {UserId}",
            isPendingDelete,
            ratings.Count,
            userId);
        return await BuildBatchResponseAsync(
            isPendingDelete ? "setPendingDelete" : "unsetPendingDelete",
            userId,
            normalizedItemIds.Count,
            ratings,
            metadata,
            cancellationToken).ConfigureAwait(false);
    }

    private static RatingListItemResponse MapListResponse(
        UserItemRating rating,
        JellyfinItemMetadata? metadata,
        IReadOnlyList<TagDefinition> tags)
    {
        return new RatingListItemResponse
        {
            ItemId = rating.ItemId.ToString("D"),
            Score = rating.Score,
            IsPendingDelete = rating.IsPendingDelete,
            LastPlayedAt = metadata?.LastPlayedAt ?? rating.LastPlayedAt,
            IsPlayed = metadata?.IsPlayed ?? rating.LastPlayedAt.HasValue,
            RatedAt = rating.RatedAt,
            UpdatedAt = rating.UpdatedAt,
            CreatedAt = rating.CreatedAt,
            ItemName = metadata?.Name,
            MediaType = metadata?.MediaType,
            ItemType = metadata?.ClientTypeName,
            ProductionYear = metadata?.ProductionYear,
            Tags = tags.Select(MapTagReference).ToList()
        };
    }

    private static RatingResponse MapSingleResponse(
        UserItemRating? rating,
        JellyfinItemMetadata metadata,
        IReadOnlyList<TagDefinition> tags)
    {
        return new RatingResponse
        {
            ItemId = metadata.ItemId.ToString("D"),
            Score = rating?.Score ?? 0,
            IsPendingDelete = rating?.IsPendingDelete ?? false,
            LastPlayedAt = metadata.LastPlayedAt ?? rating?.LastPlayedAt,
            IsPlayed = metadata.IsPlayed,
            RatedAt = rating?.RatedAt,
            UpdatedAt = rating?.UpdatedAt,
            CreatedAt = rating?.CreatedAt,
            ItemName = metadata.Name,
            MediaType = metadata.MediaType,
            ItemType = metadata.ClientTypeName,
            ProductionYear = metadata.ProductionYear,
            Tags = tags.Select(MapTagReference).ToList()
        };
    }

    private static bool MatchesCombinedFilters(UserItemRating rating, JellyfinItemMetadata? metadata, RatingQueryRequest request)
    {
        if (request.IsPlayed.HasValue)
        {
            if (metadata is null || metadata.IsPlayed != request.IsPlayed.Value)
            {
                return false;
            }
        }

        if (request.LibraryIds.Count > 0)
        {
            if (metadata is null || !MatchesLibraryFilters(metadata, request.LibraryIds))
            {
                return false;
            }
        }

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

        if (request.AddedAfterUtc.HasValue)
        {
            if (metadata?.DateCreatedUtc is null || metadata.DateCreatedUtc.Value < request.AddedAfterUtc.Value)
            {
                return false;
            }
        }

        if (request.AddedBeforeUtc.HasValue)
        {
            if (metadata?.DateCreatedUtc is null || metadata.DateCreatedUtc.Value > request.AddedBeforeUtc.Value)
            {
                return false;
            }
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

    private static bool MatchesLibraryFilters(JellyfinItemMetadata metadata, IReadOnlyList<string> libraryFilters)
    {
        foreach (string libraryFilter in libraryFilters)
        {
            if (Guid.TryParse(libraryFilter, out Guid libraryId))
            {
                if (metadata.LibraryIds.Contains(libraryId))
                {
                    return true;
                }
            }
            else if (metadata.LibraryNames.Any(name => string.Equals(name, libraryFilter, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RequiresMetadataSorting(string? sortBy)
    {
        string normalized = NormalizeSortBy(sortBy);
        return normalized is "name" or "itemname" or "year" or "productionyear" or "dateadded" or "addedat" or "lastplayedat";
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant() ?? "updatedat";
    }

    private static IEnumerable<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> SortCombined(
        IEnumerable<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> source,
        string? sortBy,
        string? sortOrder)
    {
        bool ascending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        string normalized = NormalizeSortBy(sortBy);

        if (normalized == "score")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Rating.Score).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Rating.Score).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized == "createdat")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Rating.CreatedAt).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Rating.CreatedAt).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized == "ratedat")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Rating.RatedAt ?? DateTimeOffset.MinValue).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Rating.RatedAt ?? DateTimeOffset.MinValue).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized == "lastplayedat")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Metadata?.LastPlayedAt ?? tuple.Rating.LastPlayedAt ?? DateTimeOffset.MinValue).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Metadata?.LastPlayedAt ?? tuple.Rating.LastPlayedAt ?? DateTimeOffset.MinValue).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized is "year" or "productionyear")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Metadata?.ProductionYear ?? int.MinValue).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Metadata?.ProductionYear ?? int.MinValue).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized is "dateadded" or "addedat")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Metadata?.DateCreatedUtc ?? DateTimeOffset.MinValue).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Metadata?.DateCreatedUtc ?? DateTimeOffset.MinValue).ThenByDescending(tuple => tuple.Rating.Id);
        }

        if (normalized is "name" or "itemname")
        {
            return ascending
                ? source.OrderBy(tuple => tuple.Metadata?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(tuple => tuple.Rating.Id)
                : source.OrderByDescending(tuple => tuple.Metadata?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenByDescending(tuple => tuple.Rating.Id);
        }

        return ascending
            ? source.OrderBy(tuple => tuple.Rating.UpdatedAt).ThenBy(tuple => tuple.Rating.Id)
            : source.OrderByDescending(tuple => tuple.Rating.UpdatedAt).ThenByDescending(tuple => tuple.Rating.Id);
    }

    private async Task<IReadOnlyList<RatingListItemResponse>> MapListItemsAsync(
        Guid userId,
        IReadOnlyList<UserItemRating> ratings,
        CancellationToken cancellationToken)
    {
        List<(UserItemRating Rating, JellyfinItemMetadata? Metadata)> combined = await ResolveMetadataAsync(userId, ratings, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>> tagMap = await _tagRepository
            .GetItemTagsMapAsync(userId, ratings.Select(rating => rating.ItemId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        return combined.Select(tuple => MapListResponse(
            tuple.Rating,
            tuple.Metadata,
            tagMap.TryGetValue(tuple.Rating.ItemId, out IReadOnlyList<TagDefinition>? tags)
                ? tags
                : Array.Empty<TagDefinition>())).ToList();
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

    private async Task<IReadOnlyList<JellyfinItemMetadata>> RequireMetadataAsync(
        IReadOnlyList<Guid> itemIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        List<JellyfinItemMetadata> items = [];
        foreach (Guid itemId in itemIds)
        {
            JellyfinItemMetadata metadata = await RequireMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
            items.Add(metadata);
        }

        return items;
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

    private static IReadOnlyList<Guid> NormalizeItemIds(IReadOnlyList<Guid> itemIds)
    {
        List<Guid> normalized = [];
        HashSet<Guid> seen = [];

        foreach (Guid itemId in itemIds)
        {
            if (itemId == Guid.Empty)
            {
                continue;
            }

            if (seen.Add(itemId))
            {
                normalized.Add(itemId);
            }
        }

        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one valid itemId is required.", nameof(itemIds));
        }

        return normalized;
    }

    private async Task<BatchOperationResponse> BuildBatchResponseAsync(
        string operation,
        Guid userId,
        int requestedCount,
        IReadOnlyList<UserItemRating> ratings,
        IReadOnlyList<JellyfinItemMetadata> metadata,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, JellyfinItemMetadata> metadataById = metadata.ToDictionary(item => item.ItemId, item => item);
        IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>> tagMap = await _tagRepository
            .GetItemTagsMapAsync(userId, ratings.Select(rating => rating.ItemId).ToList(), cancellationToken)
            .ConfigureAwait(false);
        List<RatingResponse> items = [];

        foreach (UserItemRating rating in ratings)
        {
            if (!metadataById.TryGetValue(rating.ItemId, out JellyfinItemMetadata? itemMetadata))
            {
                continue;
            }

            IReadOnlyList<TagDefinition> tags = tagMap.TryGetValue(rating.ItemId, out IReadOnlyList<TagDefinition>? itemTags)
                ? itemTags
                : Array.Empty<TagDefinition>();

            items.Add(MapSingleResponse(rating, itemMetadata, tags));
        }

        return new BatchOperationResponse
        {
            Operation = operation,
            RequestedCount = requestedCount,
            AffectedCount = items.Count,
            Items = items
        };
    }

    private void ValidateQueryRequest(RatingQueryRequest request)
    {
        if (request.Score.HasValue && (request.Score.Value < 0 || request.Score.Value > 5))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Score), "Score filter must be between 0 and 5.");
        }

        if (request.IsRated.HasValue)
        {
            if (!request.IsRated.Value && request.Score.HasValue && request.Score.Value > 0)
            {
                throw new ArgumentException("score>0 cannot be combined with isRated=false.", nameof(request));
            }

            if (request.IsRated.Value && request.Score.HasValue && request.Score.Value == 0)
            {
                throw new ArgumentException("score=0 cannot be combined with isRated=true.", nameof(request));
            }
        }

        if (request.AddedAfterUtc.HasValue && request.AddedBeforeUtc.HasValue && request.AddedAfterUtc.Value > request.AddedBeforeUtc.Value)
        {
            throw new ArgumentException("AddedAfterUtc cannot be later than AddedBeforeUtc.", nameof(request));
        }

        if (request.RatedAfterUtc.HasValue && request.RatedBeforeUtc.HasValue && request.RatedAfterUtc.Value > request.RatedBeforeUtc.Value)
        {
            throw new ArgumentException("RatedAfterUtc cannot be later than RatedBeforeUtc.", nameof(request));
        }

        if (request.TagIds.Any(tagId => tagId <= 0))
        {
            throw new ArgumentException("tagIds must contain positive values only.", nameof(request));
        }

        request.TagIds = request.TagIds
            .Where(tagId => tagId > 0)
            .Distinct()
            .ToList();

        if (!string.Equals(request.TagMatchMode, "any", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.TagMatchMode, "all", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("TagMatchMode must be either 'any' or 'all'.", nameof(request));
        }

        request.PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;

        int configuredDefaultPageSize = Plugin.Instance?.Configuration.DefaultPageSize ?? PluginConfiguration.DefaultPageSizeValue;
        int defaultPageSize = configuredDefaultPageSize <= 0 ? PluginConfiguration.DefaultPageSizeValue : configuredDefaultPageSize;
        request.PageSize = request.PageSize <= 0 ? defaultPageSize : Math.Min(request.PageSize, MaxPageSize);
    }

    private static TagReferenceResponse MapTagReference(TagDefinition tag)
    {
        return new TagReferenceResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            SortOrder = tag.SortOrder
        };
    }
}
