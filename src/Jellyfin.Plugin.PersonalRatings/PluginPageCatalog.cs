using System.Globalization;
using Jellyfin.Plugin.PersonalRatings.Configuration;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PersonalRatings;

internal static class PluginPageCatalog
{
    public static IEnumerable<PluginPageInfo> BuildPages(PluginConfiguration configuration, string rootNamespace, string pluginName)
    {
        List<PluginPageInfo> pages =
        [
            new PluginPageInfo
            {
                Name = "PersonalRatingsConfigPage",
                DisplayName = pluginName,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    rootNamespace)
            }
        ];

        if (configuration.EnableManagePage)
        {
            pages.Add(new PluginPageInfo
            {
                Name = "PersonalRatingsManagePage",
                DisplayName = "我的评分库",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Web.manage-page.html",
                    rootNamespace)
            });

            pages.Add(new PluginPageInfo
            {
                Name = "PersonalRatingsAuditPage",
                DisplayName = "删除审计",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Web.audit-page.html",
                    rootNamespace)
            });
        }

        return pages;
    }
}
