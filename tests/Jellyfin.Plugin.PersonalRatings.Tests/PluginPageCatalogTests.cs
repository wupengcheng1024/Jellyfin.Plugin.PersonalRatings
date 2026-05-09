using Jellyfin.Plugin.PersonalRatings.Configuration;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests;

public sealed class PluginPageCatalogTests
{
    [Fact]
    public void BuildPages_ExcludesManageAndAuditPages_WhenManagePageIsDisabled()
    {
        PluginConfiguration configuration = new()
        {
            EnableManagePage = false
        };

        IReadOnlyList<MediaBrowser.Model.Plugins.PluginPageInfo> pages = PluginPageCatalog
            .BuildPages(configuration, "Jellyfin.Plugin.PersonalRatings", "Personal Ratings")
            .ToList();

        Assert.Single(pages);
        Assert.DoesNotContain(pages, page => page.Name == "PersonalRatingsManagePage");
        Assert.DoesNotContain(pages, page => page.Name == "PersonalRatingsAuditPage");
    }

    [Fact]
    public void BuildPages_IncludesManageAndAuditPages_WhenManagePageIsEnabled()
    {
        PluginConfiguration configuration = new()
        {
            EnableManagePage = true
        };

        IReadOnlyList<MediaBrowser.Model.Plugins.PluginPageInfo> pages = PluginPageCatalog
            .BuildPages(configuration, "Jellyfin.Plugin.PersonalRatings", "Personal Ratings")
            .ToList();

        Assert.Contains(pages, page => page.Name == "PersonalRatingsManagePage");
        Assert.Contains(pages, page => page.Name == "PersonalRatingsAuditPage");
    }
}
