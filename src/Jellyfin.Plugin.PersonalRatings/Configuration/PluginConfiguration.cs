using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PersonalRatings.Configuration;

/// <summary>
/// Plugin configuration for Jellyfin Personal Ratings.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// The default SQLite file name.
    /// </summary>
    public const string DefaultDatabaseFileName = "personal-ratings.db";

    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSizeValue = 50;

    /// <summary>
    /// Gets or sets a value indicating whether favorite sync is enabled.
    /// </summary>
    public bool EnableFavoriteSync { get; set; }

    /// <summary>
    /// Gets or sets the favorite sync threshold.
    /// </summary>
    public int FavoriteThreshold { get; set; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether delete features are enabled.
    /// </summary>
    public bool EnableDeleteFeature { get; set; }

    /// <summary>
    /// Gets or sets a legacy compatibility flag for physical delete administration.
    /// Current plugin behavior always requires administrator privileges for physical deletion.
    /// </summary>
    public bool RequireAdminForPhysicalDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets the default page size for ratings queries.
    /// </summary>
    public int DefaultPageSize { get; set; } = DefaultPageSizeValue;

    /// <summary>
    /// Gets or sets a value indicating whether details page injection is enabled.
    /// </summary>
    public bool EnableDetailsPageInjection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the manage page is enabled.
    /// </summary>
    public bool EnableManagePage { get; set; }

    /// <summary>
    /// Gets or sets the SQLite database file name.
    /// </summary>
    public string DatabaseFileName { get; set; } = DefaultDatabaseFileName;
}
