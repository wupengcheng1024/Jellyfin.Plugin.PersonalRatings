namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Thrown when a plugin feature is disabled by configuration.
/// </summary>
public sealed class FeatureDisabledException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDisabledException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public FeatureDisabledException(string message)
        : base(message)
    {
    }
}
