using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Controllers;

public sealed class RatingBatchControllerTests
{
    [Fact]
    public async Task AddTags_ReturnsOk_WhenServiceSucceeds()
    {
        Guid userId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IRatingService> ratingService = new(MockBehavior.Strict);
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        Mock<IDeletionService> deletionService = new(MockBehavior.Strict);
        BatchOperationResponse response = new()
        {
            Operation = "addTags",
            RequestedCount = 1,
            AffectedCount = 1
        };

        tagService
            .Setup(service => service.BatchAddTagsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 2 && ids[0] == 1 && ids[1] == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        RatingBatchController controller = CreateController(userId, ratingService, tagService, deletionService);

        ActionResult<BatchOperationResponse> actionResult = await controller.AddTags(
            new BatchTagRequest
            {
                ItemIds = [itemId.ToString("D")],
                TagIds = [1, 2]
            },
            CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        BatchOperationResponse payload = Assert.IsType<BatchOperationResponse>(okResult.Value);
        Assert.Equal("addTags", payload.Operation);
        Assert.Equal(1, payload.AffectedCount);
    }

    [Fact]
    public async Task RemoveTags_ReturnsNotFound_WhenServiceCannotAccessItem()
    {
        Guid userId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IRatingService> ratingService = new(MockBehavior.Strict);
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        Mock<IDeletionService> deletionService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.BatchRemoveTagsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 9),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ItemNotFoundException(itemId));

        RatingBatchController controller = CreateController(userId, ratingService, tagService, deletionService);

        ActionResult<BatchOperationResponse> actionResult = await controller.RemoveTags(
            new BatchTagRequest
            {
                ItemIds = [itemId.ToString("D")],
                TagIds = [9]
            },
            CancellationToken.None);

        NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains(itemId.ToString("D"), notFoundResult.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveTags_ReturnsOk_WhenServiceSucceeds()
    {
        Guid userId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IRatingService> ratingService = new(MockBehavior.Strict);
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        Mock<IDeletionService> deletionService = new(MockBehavior.Strict);
        BatchOperationResponse response = new()
        {
            Operation = "removeTags",
            RequestedCount = 1,
            AffectedCount = 1
        };

        tagService
            .Setup(service => service.BatchRemoveTagsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        RatingBatchController controller = CreateController(userId, ratingService, tagService, deletionService);

        ActionResult<BatchOperationResponse> actionResult = await controller.RemoveTags(
            new BatchTagRequest
            {
                ItemIds = [itemId.ToString("D")],
                TagIds = [3]
            },
            CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        BatchOperationResponse payload = Assert.IsType<BatchOperationResponse>(okResult.Value);
        Assert.Equal("removeTags", payload.Operation);
        Assert.Equal(1, payload.AffectedCount);
    }

    [Fact]
    public async Task DeletePhysical_ReturnsConflict_WhenDeleteFeatureIsDisabled()
    {
        Guid userId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IRatingService> ratingService = new(MockBehavior.Strict);
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        Mock<IDeletionService> deletionService = new(MockBehavior.Strict);
        deletionService
            .Setup(service => service.DeleteItemsAsync(
                userId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeatureDisabledException("Physical delete is disabled by plugin configuration."));

        RatingBatchController controller = new(
            ratingService.Object,
            tagService.Object,
            deletionService.Object,
            NullLogger<RatingBatchController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestClaimsPrincipalFactory.CreateAuthenticatedUser(userId)
            }
        };

        ActionResult<Models.Responses.PhysicalDeleteResponse> actionResult = await controller.DeletePhysical(
            new BatchPhysicalDeleteRequest
            {
                ItemIds = [itemId.ToString("D")],
                ConfirmDelete = true
            },
            CancellationToken.None);

        ConflictObjectResult conflictResult = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        Assert.Equal("Physical delete is disabled by plugin configuration.", conflictResult.Value);
    }

    private static RatingBatchController CreateController(
        Guid userId,
        Mock<IRatingService> ratingService,
        Mock<ITagService> tagService,
        Mock<IDeletionService> deletionService)
    {
        RatingBatchController controller = new(
            ratingService.Object,
            tagService.Object,
            deletionService.Object,
            NullLogger<RatingBatchController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestClaimsPrincipalFactory.CreateAuthenticatedUser(userId)
            }
        };

        return controller;
    }
}
