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
        Assert.Contains("personalRatingsCheckboxControl", content, StringComparison.Ordinal);
        Assert.Contains("selectAllCheckbox.indeterminate", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsActionButton", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsChipButton", content, StringComparison.Ordinal);
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

        Assert.Contains("findPrimaryHeaderTabsHost", content, StringComparison.Ordinal);
        Assert.Contains("rememberHeaderTabsMarkup", content, StringComparison.Ordinal);
        Assert.Contains("restoreHeaderTabsMarkupIfNeeded", content, StringComparison.Ordinal);
        Assert.Contains("normalizeInitialRoute", content, StringComparison.Ordinal);
        Assert.Contains("tryRestorePendingBrowseRoute", content, StringComparison.Ordinal);
        Assert.Contains("redirectToNativeHomeBootstrapRoute", content, StringComparison.Ordinal);
        Assert.Contains("buildNativeHomeBootstrapUrl", content, StringComparison.Ordinal);
        Assert.Contains("findHeaderObserverTarget", content, StringComparison.Ordinal);
        Assert.Contains("findFavoritesTab", content, StringComparison.Ordinal);
        Assert.Contains("findNativeHeaderTab", content, StringComparison.Ordinal);
        Assert.Contains("getTabCandidates", content, StringComparison.Ordinal);
        Assert.Contains("buildNavButton", content, StringComparison.Ordinal);
        Assert.Contains("ensureNavButtonBehavior", content, StringComparison.Ordinal);
        Assert.Contains("handleBrowseNavClick", content, StringComparison.Ordinal);
        Assert.Contains("openPluginAdminRoute", content, StringComparison.Ordinal);
        Assert.Contains("restoreHeaderTabsMarkupIfNeeded(false);", content, StringComparison.Ordinal);
        Assert.Contains("browse-page.css?v=", content, StringComparison.Ordinal);
        Assert.Contains("page libraryPage collectionEditorPage type-interior", content, StringComparison.Ordinal);
        Assert.Contains("pageTabContent is-active personalRatingsBrowseTabContent", content, StringComparison.Ordinal);
        Assert.Contains("btnSelectView autoSize paper-icon-button-light", content, StringComparison.Ordinal);
        Assert.Contains("btnSort autoSize paper-icon-button-light", content, StringComparison.Ordinal);
        Assert.Contains("btnFilter autoSize paper-icon-button-light", content, StringComparison.Ordinal);
        Assert.Contains("btnFilter-wrapper personalRatingsBrowseFilterButtonWrap", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsBrowseFilterTray", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsBrowsePanelSection-search", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsBrowsePanelSection-sort", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsBrowsePanelSection-filterGrid", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsBrowseSheetCloseButton\" title=\"关闭\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("personalRatingsBrowseSheetCloseButton\">关闭</button>", content, StringComparison.Ordinal);
        Assert.Contains("restorePageScroll(page);", content, StringComparison.Ordinal);
        Assert.Contains(".skinHeader .headerTabs .emby-tabs-slider", content, StringComparison.Ordinal);
        Assert.Contains("insertAdjacentElement('afterend', navButton);", content, StringComparison.Ordinal);
        Assert.Contains("cleanupDuplicateNavEntries", content, StringComparison.Ordinal);
        Assert.Contains("handleHeaderTabClick", content, StringComparison.Ordinal);
        Assert.Contains("navigateToNativeHeaderTab('home');", content, StringComparison.Ordinal);
        Assert.Contains("navigateToNativeHeaderTab('favorites');", content, StringComparison.Ordinal);
        Assert.Contains("window.location.hash = '#/' + nativeHomeRoute;", content, StringComparison.Ordinal);
        Assert.Contains("window.location.replace(bootstrapUrl);", content, StringComparison.Ordinal);
        Assert.Contains("'#/' + nativeHomeRoute + '?' + nativeRouteBrowseQueryKey + '=1';", content, StringComparison.Ordinal);
        Assert.Contains("replaceHashWithoutNavigation('#/' + route);", content, StringComparison.Ordinal);
        Assert.Contains("setBrowseRouteMode(true);", content, StringComparison.Ordinal);
        Assert.Contains("setBrowseRouteMode(false);", content, StringComparison.Ordinal);
        Assert.Contains("document.body.classList.toggle('personalRatingsBrowseRouteActive', isActive);", content, StringComparison.Ordinal);
        Assert.Contains("headerObserver.observe(observerTarget", content, StringComparison.Ordinal);
        Assert.Contains("existing.previousElementSibling !== favoritesTab", content, StringComparison.Ordinal);
        Assert.Contains("navButton.dataset.personalRatingsBound = 'true';", content, StringComparison.Ordinal);
        Assert.DoesNotContain("mutationObserver.observe(document.body", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("document.body.appendChild(page);", content, StringComparison.Ordinal);
        Assert.Contains("page.remove();", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GetBrowsePageStyles_HidesNativeViews_WhenBrowseRouteIsActive()
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

        FileStreamResult result = Assert.IsType<FileStreamResult>(controller.GetBrowsePageStyles());
        string content = ReadContent(result);

        Assert.Contains(".personalRatingsBrowseRouteActive .mainAnimatedPages", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseRouteActive .skinHeader .headerTabs.hide", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseRouteActive .mainDrawer.drawer-open", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseRouteActive .mainDrawer.transition.touch-menu-la.drawer-open", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseRouteActive .tmla-mask.backdrop", content, StringComparison.Ordinal);
        Assert.Contains("overflow: hidden !important;", content, StringComparison.Ordinal);
        Assert.Contains("backdrop-filter: none !important;", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseRouteActive .itemDetailPage", content, StringComparison.Ordinal);
        Assert.Contains("visibility: hidden !important;", content, StringComparison.Ordinal);
        Assert.Contains("pointer-events: none !important;", content, StringComparison.Ordinal);
        Assert.Contains("display: flex !important;", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseCommandBar", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseFilterButtonWrap", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseFilterSheet", content, StringComparison.Ordinal);
        Assert.Contains("position: absolute;", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseFilterTray[hidden]", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowsePanelSection[hidden]", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseTagMatchField[hidden]", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsBrowseResults", content, StringComparison.Ordinal);
        Assert.Contains("display: block !important;", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsMediaCard .cardBox", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsListItem", content, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDetailPanelScript_ContainsQuickCreateTagAndIconCloseContracts()
    {
        WebAssetsController controller = new(
            new TestFeatureService
            {
                IsDetailsPageInjectionEnabled = true
            },
            NullLogger<WebAssetsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        FileStreamResult result = Assert.IsType<FileStreamResult>(controller.GetDetailsPanelScript());
        string content = ReadContent(result);

        Assert.Contains("personalRatingsTagCreateForm", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsTagCreateInput", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsTagCreateSubmit", content, StringComparison.Ordinal);
        Assert.Contains("personalRatingsTagCreateMessage", content, StringComparison.Ordinal);
        Assert.Contains("renderTagCreateMessage", content, StringComparison.Ordinal);
        Assert.Contains("setTagCreateBusy", content, StringComparison.Ordinal);
        Assert.Contains("clearTagCreateInput", content, StringComparison.Ordinal);
        Assert.Contains("material-icons\" aria-hidden=\"true\">close</span>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("personalRatingsPanelCloseButton\">关闭</button>", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GetManagePageStyles_ContainsReadableAdminButtonAndCheckboxContracts()
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

        FileStreamResult result = Assert.IsType<FileStreamResult>(controller.GetManagePageStyles());
        string content = ReadContent(result);

        Assert.Contains(".personalRatingsButton", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsButtonPrimary", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsCheckboxControl", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsCheckboxMark", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsActionButton", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsTableWrap", content, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsStatusText.is-success", content, StringComparison.Ordinal);
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
        string detailsPanelContent = ReadContent(Assert.IsType<FileStreamResult>(controller.GetDetailsPanelScript()));

        Assert.Contains("window.PersonalRatingsFeatureStateCache", browseApiContent, StringComparison.Ordinal);
        Assert.Contains("window.PersonalRatingsFeatureStateCache", detailsApiContent, StringComparison.Ordinal);
        Assert.DoesNotContain("MutationObserver", detailsRatingContent, StringComparison.Ordinal);
        Assert.Contains("window.PersonalRatingsDetailPanel.hideLauncher();", detailsRatingContent, StringComparison.Ordinal);
        Assert.Contains("var syncTimerIds = [];", detailsRatingContent, StringComparison.Ordinal);
        Assert.Contains("personalRatingsInlineControls", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("personalRatingsQuickButton", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("personalRatingsInlineSummary", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("personalRatingsPanelCloseButton", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("renderInlineSummary", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("setPanelOpen", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains("togglePanel", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsDetailPanel .button-flat", detailsPanelContent, StringComparison.Ordinal);
        Assert.Contains(".personalRatingsManageButton", detailsPanelContent, StringComparison.Ordinal);
    }

    private static string ReadContent(FileStreamResult result)
    {
        using StreamReader reader = new(result.FileStream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}
