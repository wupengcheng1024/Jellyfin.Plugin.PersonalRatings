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

public sealed class TagsControllerTests
{
    [Fact]
    public async Task GetTags_ReturnsUnauthorized_WhenUserContextIsMissing()
    {
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        TagsController controller = new(tagService.Object, NullLogger<TagsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        ActionResult<IReadOnlyList<TagDefinitionResponse>> actionResult = await controller.GetTags(includeDisabled: false, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetTags_ReturnsForbid_WhenServiceRejectsIncludeDisabled()
    {
        Guid userId = Guid.NewGuid();
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.GetTagDefinitionsAsync(userId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only administrators can view disabled tags."));

        TagsController controller = CreateController(tagService.Object, userId);

        ActionResult<IReadOnlyList<TagDefinitionResponse>> actionResult = await controller.GetTags(includeDisabled: true, CancellationToken.None);

        Assert.IsType<ForbidResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetTags_ReturnsOk_WhenServiceReturnsEmptyList()
    {
        Guid userId = Guid.NewGuid();
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.GetTagDefinitionsAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TagDefinitionResponse>());

        TagsController controller = CreateController(tagService.Object, userId);

        ActionResult<IReadOnlyList<TagDefinitionResponse>> actionResult = await controller.GetTags(includeDisabled: false, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        IReadOnlyList<TagDefinitionResponse> payload = Assert.IsAssignableFrom<IReadOnlyList<TagDefinitionResponse>>(okResult.Value);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task CreateTag_ReturnsOk_WhenServiceCreatesDefinition()
    {
        Guid userId = Guid.NewGuid();
        CreateTagDefinitionRequest request = new()
        {
            Name = "重看",
            Color = "#d88b2f",
            SortOrder = 10,
            IsEnabled = true
        };
        TagDefinitionResponse response = new()
        {
            Id = 1,
            Name = request.Name,
            Color = request.Color,
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        Mock<ITagService> tagService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.CreateTagDefinitionAsync(userId, It.Is<CreateTagDefinitionRequest>(item => item.Name == request.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        TagsController controller = CreateController(tagService.Object, userId);

        ActionResult<TagDefinitionResponse> actionResult = await controller.CreateTag(request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        TagDefinitionResponse payload = Assert.IsType<TagDefinitionResponse>(okResult.Value);
        Assert.Equal(response.Name, payload.Name);
        Assert.True(payload.IsEnabled);
    }

    [Fact]
    public async Task UpdateTag_ReturnsNotFound_WhenServiceCannotResolveTag()
    {
        Guid userId = Guid.NewGuid();
        UpdateTagDefinitionRequest request = new()
        {
            Name = "年度候选",
            Color = "#7aa1d2",
            SortOrder = 20,
            IsEnabled = true
        };

        Mock<ITagService> tagService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.UpdateTagDefinitionAsync(userId, 42, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("The target tag definition does not exist."));

        TagsController controller = CreateController(tagService.Object, userId);

        ActionResult<TagDefinitionResponse> actionResult = await controller.UpdateTag(42, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task DeleteTag_ReturnsNoContent_WhenServiceDeletesDefinition()
    {
        Guid userId = Guid.NewGuid();
        Mock<ITagService> tagService = new(MockBehavior.Strict);
        tagService
            .Setup(service => service.DeleteTagDefinitionAsync(userId, 7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TagsController controller = CreateController(tagService.Object, userId);

        ActionResult actionResult = await controller.DeleteTag(7, CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
    }

    private static TagsController CreateController(ITagService tagService, Guid userId)
    {
        TagsController controller = new(tagService, NullLogger<TagsController>.Instance);
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
