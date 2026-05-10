using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Services;

public sealed class TagServiceTests
{
    [Fact]
    public async Task CreateTagDefinitionAsync_ThrowsArgumentException_WhenSameNameAlreadyExistsIgnoringCase()
    {
        Guid userId = Guid.NewGuid();
        TagDefinition existing = new()
        {
            Id = 12,
            Name = "日本",
            Color = "#d88b2f",
            SortOrder = 0,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        CreateTagDefinitionRequest request = new()
        {
            Name = "  日本  ",
            Color = "#ffffff",
            SortOrder = 0,
            IsEnabled = true
        };

        Mock<ITagRepository> tagRepository = new(MockBehavior.Strict);
        tagRepository
            .Setup(repository => repository.GetDefinitionByNameAsync("日本", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IJellyfinItemResolver> itemResolver = new(MockBehavior.Strict);
        Mock<IUserManager> userManager = CreateUserManager(userId, isAdministrator: true);
        TagService service = new(
            tagRepository.Object,
            ratingRepository.Object,
            itemResolver.Object,
            userManager.Object,
            NullLogger<TagService>.Instance);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTagDefinitionAsync(userId, request, CancellationToken.None));

        Assert.Contains("same name already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateTagDefinitionAsync_ThrowsArgumentException_WhenRenamingToExistingNameIgnoringCase()
    {
        Guid userId = Guid.NewGuid();
        TagDefinition current = new()
        {
            Id = 7,
            Name = "动作",
            Color = "#d88b2f",
            SortOrder = 10,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        TagDefinition other = new()
        {
            Id = 11,
            Name = "日本",
            Color = "#d88b2f",
            SortOrder = 20,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        UpdateTagDefinitionRequest request = new()
        {
            Name = "  日本  ",
            Color = "#d88b2f",
            SortOrder = 10,
            IsEnabled = true
        };

        Mock<ITagRepository> tagRepository = new(MockBehavior.Strict);
        tagRepository
            .Setup(repository => repository.GetDefinitionAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        tagRepository
            .Setup(repository => repository.GetDefinitionByNameAsync("日本", It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IJellyfinItemResolver> itemResolver = new(MockBehavior.Strict);
        Mock<IUserManager> userManager = CreateUserManager(userId, isAdministrator: true);
        TagService service = new(
            tagRepository.Object,
            ratingRepository.Object,
            itemResolver.Object,
            userManager.Object,
            NullLogger<TagService>.Instance);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateTagDefinitionAsync(userId, 7, request, CancellationToken.None));

        Assert.Contains("same name already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BatchAddTagsAsync_ReturnsUpdatedItemsWithTags()
    {
        Guid userId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        TagDefinition tag = new()
        {
            Id = 1,
            Name = "重看",
            Color = "#d88b2f",
            SortOrder = 10,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        JellyfinItemMetadata metadata = new()
        {
            ItemId = itemId,
            Name = "Example Movie",
            ClientTypeName = "Movie",
            MediaType = "Video",
            ProductionYear = 2024,
            IsPlayed = true
        };
        UserItemRating rating = new()
        {
            ItemId = itemId,
            UserId = userId,
            Score = 4,
            IsPendingDelete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RatedAt = DateTimeOffset.UtcNow
        };

        Mock<ITagRepository> tagRepository = new(MockBehavior.Strict);
        tagRepository
            .Setup(repository => repository.GetDefinitionsByIdsAsync(
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([tag]);
        tagRepository
            .Setup(repository => repository.AddTagsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 1),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        tagRepository
            .Setup(repository => repository.GetItemTagsMapAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<TagDefinition>>
            {
                [itemId] = [tag]
            });

        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.EnsureRowsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([rating]);
        ratingRepository
            .Setup(repository => repository.GetManyAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([rating]);

        Mock<IJellyfinItemResolver> itemResolver = new(MockBehavior.Strict);
        itemResolver
            .Setup(resolver => resolver.GetMetadataAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([metadata]);

        Mock<IUserManager> userManager = new(MockBehavior.Loose);
        TagService service = new(
            tagRepository.Object,
            ratingRepository.Object,
            itemResolver.Object,
            userManager.Object,
            NullLogger<TagService>.Instance);

        BatchOperationResponse response = await service.BatchAddTagsAsync(userId, [itemId], [1], CancellationToken.None);

        Assert.Equal("addTags", response.Operation);
        Assert.Equal(1, response.AffectedCount);
        RatingResponse item = Assert.Single(response.Items);
        TagReferenceResponse assignedTag = Assert.Single(item.Tags);
        Assert.Equal("重看", assignedTag.Name);
    }

    [Fact]
    public async Task BatchAddTagsAsync_ThrowsArgumentException_WhenTagIdsAreEmpty()
    {
        Mock<ITagRepository> tagRepository = new(MockBehavior.Strict);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IJellyfinItemResolver> itemResolver = new(MockBehavior.Strict);
        Mock<IUserManager> userManager = new(MockBehavior.Loose);
        TagService service = new(
            tagRepository.Object,
            ratingRepository.Object,
            itemResolver.Object,
            userManager.Object,
            NullLogger<TagService>.Instance);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.BatchAddTagsAsync(Guid.NewGuid(), [Guid.NewGuid()], Array.Empty<long>(), CancellationToken.None));

        Assert.Contains("tagId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Mock<IUserManager> CreateUserManager(Guid userId, bool isAdministrator)
    {
        User user = new("tag-test-user", "tag-auth", "tag-reset")
        {
            Id = userId
        };
        user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);

        Mock<IUserManager> userManager = new(MockBehavior.Strict);
        userManager
            .Setup(manager => manager.GetUserById(userId))
            .Returns(user);

        return userManager;
    }
}
