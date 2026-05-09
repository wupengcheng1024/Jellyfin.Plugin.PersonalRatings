using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
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
    }
}
