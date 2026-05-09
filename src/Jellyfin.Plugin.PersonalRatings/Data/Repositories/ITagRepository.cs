using Jellyfin.Plugin.PersonalRatings.Models.Entities;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal interface ITagRepository
{
    Task<IReadOnlyList<TagDefinition>> ListDefinitionsAsync(bool includeDisabled, CancellationToken cancellationToken);

    Task<IReadOnlyList<TagDefinition>> GetDefinitionsByIdsAsync(IReadOnlyList<long> tagIds, CancellationToken cancellationToken);

    Task<TagDefinition?> GetDefinitionAsync(long tagId, CancellationToken cancellationToken);

    Task<TagDefinition> CreateDefinitionAsync(TagDefinition definition, CancellationToken cancellationToken);

    Task<TagDefinition?> UpdateDefinitionAsync(TagDefinition definition, CancellationToken cancellationToken);

    Task<bool> DeleteDefinitionAsync(long tagId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TagDefinition>> GetItemTagsAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>>> GetItemTagsMapAsync(
        Guid userId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task ReplaceItemTagsAsync(Guid userId, Guid itemId, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);

    Task AddTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);

    Task RemoveTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken);
}
