namespace Jellyfin.Plugin.PersonalRatings.Models.Responses;

/// <summary>
/// Read-only feature-switch snapshot for plugin web clients.
/// </summary>
public sealed class PluginFeaturesResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether physical delete is enabled.
    /// </summary>
    public bool IsDeleteFeatureEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether details-page injection is enabled.
    /// </summary>
    public bool IsDetailsPageInjectionEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the management page is enabled.
    /// </summary>
    public bool IsManagePageEnabled { get; set; }
}
