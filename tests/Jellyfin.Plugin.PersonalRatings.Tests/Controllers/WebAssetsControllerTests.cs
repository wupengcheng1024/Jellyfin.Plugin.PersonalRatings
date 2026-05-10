using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
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
        Assert.IsType<FileStreamResult>(controller.GetBrowseShellScript());
        Assert.IsType<FileStreamResult>(controller.GetBrowsePageStyles());
    }

    [Fact]
    public void GetManagePageScript_ContainsTagFilterQueryAndEmptyStateContracts()
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

        FileStreamResult result = Assert.IsType<FileStreamResult>(controller.GetManagePageScript());
        string content = ReadContent(result);

        Assert.Contains("request.tagIds = state.selectedFilterTagIds.slice();", content, StringComparison.Ordinal);
        Assert.Contains("request.tagMatchMode = state.tagMatchMode || 'any';", content, StringComparison.Ordinal);
        Assert.Contains("当前筛选条件没有命中记录", content, StringComparison.Ordinal);
        Assert.Contains("当前还没有启用标签。请先到标签管理页创建并启用后再筛选。", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GetBrowseShellScript_UsesSingleNavInjectionAndDestroyLifecycleContracts()
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

        FileStreamResult result = Assert.IsType<FileStreamResult>(controller.GetBrowseShellScript());
        string content = ReadContent(result);

        Assert.DoesNotContain("MutationObserver", content, StringComparison.Ordinal);
        Assert.Contains("findPrimaryHeaderTabsHost", content, StringComparison.Ordinal);
        Assert.Contains("findFavoritesTab", content, StringComparison.Ordinal);
        Assert.Contains("insertAdjacentElement('afterend', link);", content, StringComparison.Ordinal);
        Assert.Contains("cleanupDuplicateNavEntries", content, StringComparison.Ordinal);
        Assert.Contains("document.body.appendChild(page);", content, StringComparison.Ordinal);
        Assert.Contains("page.remove();", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FeatureScripts_UseSharedFeatureStateCache_AndAvoidGlobalMutationObservers()
    {
        WebAssetsController controller = new(
            new TestFeatureService
            {
                IsManagePageEnabled = true,
                IsDetailsPageInjectionEnabled = true
            },
            NullLogger<WebAssetsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        string browseApiContent = ReadContent(Assert.IsType<FileStreamResult>(controller.GetBrowseApiScript()));
        string detailsApiContent = ReadContent(Assert.IsType<FileStreamResult>(controller.GetDetailsApiScript()));
        string detailsRatingContent = ReadContent(Assert.IsType<FileStreamResult>(controller.GetDetailsRatingScript()));

        Assert.Contains("window.PersonalRatingsFeatureStateCache", browseApiContent, StringComparison.Ordinal);
        Assert.Contains("window.PersonalRatingsFeatureStateCache", detailsApiContent, StringComparison.Ordinal);
        Assert.DoesNotContain("MutationObserver", detailsRatingContent, StringComparison.Ordinal);
        Assert.Contains("window.PersonalRatingsDetailPanel.hideLauncher();", detailsRatingContent, StringComparison.Ordinal);
        Assert.Contains("var syncTimerIds = [];", detailsRatingContent, StringComparison.Ordinal);
    }

    private static string ReadContent(FileStreamResult result)
    {
        using StreamReader reader = new(result.FileStream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}
