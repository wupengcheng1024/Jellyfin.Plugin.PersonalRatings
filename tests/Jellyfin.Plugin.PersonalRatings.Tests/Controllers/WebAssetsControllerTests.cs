using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Controllers;

public sealed class WebAssetsControllerTests
{
    [Fact]
    public void GetDetailsRatingScript_ReturnsNotFound_WhenDetailsInjectionIsDisabled()
    {
        WebAssetsController controller = new(
            new TestFeatureService
            {
                IsDetailsPageInjectionEnabled = false
            },
            NullLogger<WebAssetsController>.Instance);

        ActionResult result = controller.GetDetailsRatingScript();

        Assert.IsType<NotFoundResult>(result);
        Assert.IsType<NotFoundResult>(controller.GetDetailsApiScript());
        Assert.IsType<NotFoundResult>(controller.GetDetailsPanelScript());
    }

    [Fact]
    public void GetManageAssets_ReturnNotFound_WhenManagePageIsDisabled()
    {
        WebAssetsController controller = new(
            new TestFeatureService
            {
                IsManagePageEnabled = false
            },
            NullLogger<WebAssetsController>.Instance);

        Assert.IsType<NotFoundResult>(controller.GetManagePageScript());
        Assert.IsType<NotFoundResult>(controller.GetManagePageStyles());
        Assert.IsType<NotFoundResult>(controller.GetAuditPageScript());
        Assert.IsType<NotFoundResult>(controller.GetTagManagePageScript());
        Assert.IsType<NotFoundResult>(controller.GetTagManagePageStyles());
        Assert.IsType<NotFoundResult>(controller.GetBrowseStateScript());
        Assert.IsType<NotFoundResult>(controller.GetBrowseApiScript());
        Assert.IsType<NotFoundResult>(controller.GetBrowseRenderScript());
        Assert.IsType<NotFoundResult>(controller.GetBrowseFiltersScript());
        Assert.IsType<NotFoundResult>(controller.GetBrowseShellScript());
        Assert.IsType<NotFoundResult>(controller.GetBrowsePageStyles());
    }

    [Fact]
    public void GetManageAssets_ReturnEmbeddedFiles_WhenManagePageIsEnabled()
    {
        WebAssetsController controller = new(
            new TestFeatureService
            {
                IsManagePageEnabled = true
            },
            NullLogger<WebAssetsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        Assert.IsType<FileStreamResult>(controller.GetManagePageScript());
        Assert.IsType<FileStreamResult>(controller.GetManagePageStyles());
    }
}
