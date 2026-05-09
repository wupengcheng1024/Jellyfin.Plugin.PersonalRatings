using Jellyfin.Plugin.PersonalRatings.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Moq;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Helpers;

public static class TestPluginFactory
{
    public static Plugin Create(Action<PluginConfiguration>? configure = null)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "Jellyfin.PersonalRatings.Tests");
        Mock<IApplicationPaths> applicationPaths = new();
        applicationPaths.SetupGet(path => path.ProgramDataPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.WebPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.ProgramSystemPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.DataPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.ImageCachePath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.PluginsPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.PluginConfigurationsPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.LogDirectoryPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.ConfigurationDirectoryPath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.SystemConfigurationFilePath).Returns(Path.Combine(tempPath, "system.xml"));
        applicationPaths.SetupGet(path => path.CachePath).Returns(tempPath);
        applicationPaths.SetupGet(path => path.TempDirectory).Returns(tempPath);
        applicationPaths.SetupGet(path => path.VirtualDataPath).Returns(tempPath);

        Mock<IXmlSerializer> xmlSerializer = new();

        Plugin plugin = new(applicationPaths.Object, xmlSerializer.Object);
        if (plugin.Configuration is null)
        {
            plugin.UpdateConfiguration(new PluginConfiguration());
        }

        PluginConfiguration configuration = plugin.Configuration
            ?? throw new InvalidOperationException("Plugin configuration could not be initialized for tests.");

        configure?.Invoke(configuration);
        return plugin;
    }
}
