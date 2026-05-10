using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class TagService : ITagService
{
    private const string DefaultTagColor = "#d88b2f";
    private readonly IJellyfinItemResolver _itemResolver;
    private readonly ILogger<TagService> _logger;
    private readonly IRatingRepository _ratingRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUserManager _userManager;

    public TagService(
        ITagRepository tagRepository,
        IRatingRepository ratingRepository,
        IJellyfinItemResolver itemResolver,
        IUserManager userManager,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _ratingRepository = ratingRepository;
        _itemResolver = itemResolver;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TagDefinitionResponse>> GetTagDefinitionsAsync(
        Guid operatorUserId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        if (includeDisabled)
        {
            EnsureAdministrator(operatorUserId);
        }

        IReadOnlyList<TagDefinition> definitions = await _tagRepository
            .ListDefinitionsAsync(includeDisabled, cancellationToken)
            .ConfigureAwait(false);

        return definitions.Select(MapDefinition).ToList();
    }

    public async Task<TagDefinitionResponse> CreateTagDefinitionAsync(
        Guid operatorUserId,
        CreateTagDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdministrator(operatorUserId);

        string normalizedName = NormalizeTagName(request.Name);
        await EnsureTagNameAvailableAsync(normalizedName, null, cancellationToken).ConfigureAwait(false);

        TagDefinition definition = new()
        {
            Name = normalizedName,
            Color = NormalizeTagColor(request.Color),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            TagDefinition created = await _tagRepository.CreateDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Created tag definition {TagName} ({TagId})", created.Name, created.Id);
            return MapDefinition(created);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ArgumentException("A tag with the same name already exists.", nameof(request), exception);
        }
    }

    public async Task<TagDefinitionResponse> UpdateTagDefinitionAsync(
        Guid operatorUserId,
        long tagId,
        UpdateTagDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdministrator(operatorUserId);

        TagDefinition? existing = await _tagRepository.GetDefinitionAsync(tagId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException("The requested tag definition does not exist.");
        }

        string normalizedName = NormalizeTagName(request.Name);
        await EnsureTagNameAvailableAsync(normalizedName, existing.Id, cancellationToken).ConfigureAwait(false);

        existing.Name = normalizedName;
        existing.Color = NormalizeTagColor(request.Color);
        existing.SortOrder = request.SortOrder;
        existing.IsEnabled = request.IsEnabled;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            TagDefinition? updated = await _tagRepository.UpdateDefinitionAsync(existing, cancellationToken).ConfigureAwait(false);
            if (updated is null)
            {
                throw new KeyNotFoundException("The requested tag definition does not exist.");
            }

            _logger.LogInformation("Updated tag definition {TagName} ({TagId})", updated.Name, updated.Id);
            return MapDefinition(updated);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ArgumentException("A tag with the same name already exists.", nameof(request), exception);
        }
    }

    public async Task DeleteTagDefinitionAsync(Guid operatorUserId, long tagId, CancellationToken cancellationToken)
    {
        EnsureAdministrator(operatorUserId);

        bool deleted = await _tagRepository.DeleteDefinitionAsync(tagId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
        {
            throw new KeyNotFoundException("The requested tag definition does not exist.");
        }

        _logger.LogInformation("Deleted tag definition {TagId}", tagId);
    }

    public async Task<ItemTagsResponse> GetItemTagsAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        await RequireAccessibleMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetItemTagsAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return new ItemTagsResponse
        {
            ItemId = itemId.ToString("D"),
            Tags = tags.Select(MapReference).ToList()
        };
    }

    public async Task<ItemTagsResponse> ReplaceItemTagsAsync(Guid userId, Guid itemId, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        await RequireAccessibleMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<long> normalizedTagIds = NormalizeTagIds(tagIds);
        await RequireEnabledTagsAsync(normalizedTagIds, cancellationToken).ConfigureAwait(false);
        await _ratingRepository.EnsureRowsAsync(userId, [itemId], cancellationToken).ConfigureAwait(false);
        await _tagRepository.ReplaceItemTagsAsync(userId, itemId, normalizedTagIds, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetItemTagsAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Replaced {Count} tags for item {ItemId} and user {UserId}", tags.Count, itemId, userId);

        return new ItemTagsResponse
        {
            ItemId = itemId.ToString("D"),
            Tags = tags.Select(MapReference).ToList()
        };
    }

    public async Task<BatchOperationResponse> BatchAddTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        IReadOnlyList<long> normalizedTagIds = NormalizeTagIds(tagIds);
        if (normalizedTagIds.Count == 0)
        {
            throw new ArgumentException("At least one valid tagId is required.", nameof(tagIds));
        }

        IReadOnlyList<JellyfinItemMetadata> metadata = await RequireAccessibleMetadataAsync(normalizedItemIds, userId, cancellationToken).ConfigureAwait(false);
        await RequireEnabledTagsAsync(normalizedTagIds, cancellationToken).ConfigureAwait(false);
        await _ratingRepository.EnsureRowsAsync(userId, normalizedItemIds, cancellationToken).ConfigureAwait(false);
        await _tagRepository.AddTagsAsync(userId, normalizedItemIds, normalizedTagIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added {TagCount} tags to {ItemCount} items for user {UserId}", normalizedTagIds.Count, normalizedItemIds.Count, userId);
        return await BuildBatchResponseAsync("addTags", userId, normalizedItemIds, metadata, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BatchOperationResponse> BatchRemoveTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        IReadOnlyList<long> normalizedTagIds = NormalizeTagIds(tagIds);
        if (normalizedTagIds.Count == 0)
        {
            throw new ArgumentException("At least one valid tagId is required.", nameof(tagIds));
        }

        IReadOnlyList<JellyfinItemMetadata> metadata = await RequireAccessibleMetadataAsync(normalizedItemIds, userId, cancellationToken).ConfigureAwait(false);
        await _tagRepository.RemoveTagsAsync(userId, normalizedItemIds, normalizedTagIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed {TagCount} tags from {ItemCount} items for user {UserId}", normalizedTagIds.Count, normalizedItemIds.Count, userId);
        return await BuildBatchResponseAsync("removeTags", userId, normalizedItemIds, metadata, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureAdministrator(Guid operatorUserId)
    {
        User? operatorUser = _userManager.GetUserById(operatorUserId);
        if (operatorUser is null || !operatorUser.HasPermission(PermissionKind.IsAdministrator))
        {
            throw new UnauthorizedAccessException("Tag definition management requires administrator privileges.");
        }
    }

    private async Task EnsureTagNameAvailableAsync(string normalizedName, long? currentTagId, CancellationToken cancellationToken)
    {
        TagDefinition? existing = await _tagRepository.GetDefinitionByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        if (currentTagId.HasValue && existing.Id == currentTagId.Value)
        {
            return;
        }

        throw new ArgumentException("A tag with the same name already exists.", nameof(normalizedName));
    }

    private async Task<JellyfinItemMetadata> RequireAccessibleMetadataAsync(Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        JellyfinItemMetadata? metadata = await _itemResolver.GetMetadataAsync(itemId, userId, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            throw new ItemNotFoundException(itemId);
        }

        return metadata;
    }

    private async Task<IReadOnlyList<JellyfinItemMetadata>> RequireAccessibleMetadataAsync(
        IReadOnlyList<Guid> itemIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<JellyfinItemMetadata> items = await _itemResolver.GetMetadataAsync(itemIds, userId, cancellationToken).ConfigureAwait(false);
        if (items.Count != itemIds.Count)
        {
            HashSet<Guid> resolvedIds = items.Select(item => item.ItemId).ToHashSet();
            Guid missingId = itemIds.First(itemId => !resolvedIds.Contains(itemId));
            throw new ItemNotFoundException(missingId);
        }

        return items;
    }

    private async Task<IReadOnlyList<TagDefinition>> RequireEnabledTagsAsync(IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return Array.Empty<TagDefinition>();
        }

        IReadOnlyList<TagDefinition> tags = await _tagRepository.GetDefinitionsByIdsAsync(tagIds, cancellationToken).ConfigureAwait(false);
        if (tags.Count != tagIds.Count)
        {
            throw new ArgumentException("One or more tagIds do not exist.", nameof(tagIds));
        }

        if (tags.Any(tag => !tag.IsEnabled))
        {
            throw new ArgumentException("Only enabled tags can be assigned to items.", nameof(tagIds));
        }

        return tags;
    }

    private async Task<BatchOperationResponse> BuildBatchResponseAsync(
        string operation,
        Guid userId,
        IReadOnlyList<Guid> itemIds,
        IReadOnlyList<JellyfinItemMetadata> metadata,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserItemRating> ratings = await _ratingRepository.GetManyAsync(userId, itemIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>> tagMap = await _tagRepository
            .GetItemTagsMapAsync(userId, itemIds, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, JellyfinItemMetadata> metadataById = metadata.ToDictionary(item => item.ItemId, item => item);
        List<RatingResponse> items = [];

        foreach (UserItemRating rating in ratings)
        {
            if (!metadataById.TryGetValue(rating.ItemId, out JellyfinItemMetadata? itemMetadata))
            {
                continue;
            }

            IReadOnlyList<TagDefinition> itemTags = tagMap.TryGetValue(rating.ItemId, out IReadOnlyList<TagDefinition>? tags)
                ? tags
                : Array.Empty<TagDefinition>();

            items.Add(MapRatingResponse(rating, itemMetadata, itemTags));
        }

        return new BatchOperationResponse
        {
            Operation = operation,
            RequestedCount = itemIds.Count,
            AffectedCount = items.Count,
            Items = items
        };
    }

    private static RatingResponse MapRatingResponse(
        UserItemRating rating,
        JellyfinItemMetadata metadata,
        IReadOnlyList<TagDefinition> tags)
    {
        return new RatingResponse
        {
            ItemId = metadata.ItemId.ToString("D"),
            Score = rating.Score,
            IsPendingDelete = rating.IsPendingDelete,
            LastPlayedAt = metadata.LastPlayedAt ?? rating.LastPlayedAt,
            IsPlayed = metadata.IsPlayed,
            RatedAt = rating.RatedAt,
            UpdatedAt = rating.UpdatedAt,
            CreatedAt = rating.CreatedAt,
            ItemName = metadata.Name,
            MediaType = metadata.MediaType,
            ItemType = metadata.ClientTypeName,
            ProductionYear = metadata.ProductionYear,
            Tags = tags.Select(MapReference).ToList()
        };
    }

    private static TagDefinitionResponse MapDefinition(TagDefinition definition)
    {
        return new TagDefinitionResponse
        {
            Id = definition.Id,
            Name = definition.Name,
            Color = definition.Color,
            SortOrder = definition.SortOrder,
            IsEnabled = definition.IsEnabled,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt
        };
    }

    private static TagReferenceResponse MapReference(TagDefinition definition)
    {
        return new TagReferenceResponse
        {
            Id = definition.Id,
            Name = definition.Name,
            Color = definition.Color,
            SortOrder = definition.SortOrder
        };
    }

    private static string NormalizeTagName(string value)
    {
        string normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Tag name is required.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeTagColor(string? value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? DefaultTagColor : value.Trim();
        return normalized;
    }

    private static IReadOnlyList<long> NormalizeTagIds(IReadOnlyList<long> tagIds)
    {
        List<long> normalized = [];
        HashSet<long> seen = [];

        foreach (long tagId in tagIds)
        {
            if (tagId <= 0)
            {
                continue;
            }

            if (seen.Add(tagId))
            {
                normalized.Add(tagId);
            }
        }

        return normalized;
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
}
