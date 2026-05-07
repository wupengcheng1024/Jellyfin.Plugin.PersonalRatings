namespace Jellyfin.Plugin.PersonalRatings.Data;

internal interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
