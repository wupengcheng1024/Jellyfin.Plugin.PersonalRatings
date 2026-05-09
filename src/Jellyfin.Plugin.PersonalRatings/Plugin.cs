using System.Globalization;
using Jellyfin.Plugin.PersonalRatings.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PersonalRatings;

/// <summary>
/// The main plugin entry point.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The Jellyfin application paths.</param>
    /// <param name="xmlSerializer">The Jellyfin XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        WebAssetVersionToken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Gets the cache-busting token used for plugin web assets during the current Jellyfin process lifetime.
    /// </summary>
    public string WebAssetVersionToken { get; }

    /// <inheritdoc />
    public override string Name => "Personal Ratings";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("899735be-ba58-4268-b6a2-f07fc0f0c807");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return PluginPageCatalog.BuildPages(
            Configuration,
            GetType().Namespace ?? nameof(Jellyfin.Plugin.PersonalRatings),
            Name);
    }
}
