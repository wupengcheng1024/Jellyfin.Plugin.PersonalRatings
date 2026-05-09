using Jellyfin.Plugin.PersonalRatings.Configuration;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class PluginFeatureService : IPluginFeatureService
{
    public bool IsDeleteFeatureEnabled => GetConfiguration().EnableDeleteFeature;

    public bool IsDetailsPageInjectionEnabled => GetConfiguration().EnableDetailsPageInjection;

    public bool IsManagePageEnabled => GetConfiguration().EnableManagePage;

    private static PluginConfiguration GetConfiguration()
    {
        PluginConfiguration? configuration = Plugin.Instance?.Configuration;
        return configuration ?? new PluginConfiguration();
    }
}
