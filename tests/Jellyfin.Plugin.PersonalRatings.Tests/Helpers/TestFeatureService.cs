using Jellyfin.Plugin.PersonalRatings.Services;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Helpers;

public sealed class TestFeatureService : IPluginFeatureService
{
    public bool IsDeleteFeatureEnabled { get; set; } = true;

    public bool IsDetailsPageInjectionEnabled { get; set; } = true;

    public bool IsManagePageEnabled { get; set; } = true;
}
