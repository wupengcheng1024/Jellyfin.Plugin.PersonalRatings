namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Provides read-only access to plugin feature switches.
/// </summary>
public interface IPluginFeatureService
{
    /// <summary>
    /// Gets a value indicating whether physical delete features are enabled.
    /// </summary>
    bool IsDeleteFeatureEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether details-page script injection is enabled.
    /// </summary>
    bool IsDetailsPageInjectionEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the management page is enabled.
    /// </summary>
    bool IsManagePageEnabled { get; }
}
