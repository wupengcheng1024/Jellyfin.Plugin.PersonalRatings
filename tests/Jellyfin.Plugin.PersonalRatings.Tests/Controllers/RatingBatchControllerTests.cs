using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
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
}
