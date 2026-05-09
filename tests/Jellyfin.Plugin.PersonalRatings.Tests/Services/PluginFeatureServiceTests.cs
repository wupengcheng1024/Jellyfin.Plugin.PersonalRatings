using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Services;

public sealed class PluginFeatureServiceTests
{
    [Fact]
    public void FeatureFlags_ReflectCurrentPluginConfiguration()
    {
        TestPluginFactory.Create(configuration =>
        {
            configuration.EnableDeleteFeature = false;
            configuration.EnableDetailsPageInjection = false;
            configuration.EnableManagePage = false;
            configuration.RequireAdminForPhysicalDelete = false;
        });

        PluginFeatureService service = new();

        Assert.False(service.IsDeleteFeatureEnabled);
        Assert.False(service.IsDetailsPageInjectionEnabled);
        Assert.False(service.IsManagePageEnabled);
    }
}
