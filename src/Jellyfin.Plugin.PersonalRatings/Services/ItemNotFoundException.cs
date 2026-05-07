namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class ItemNotFoundException : Exception
{
    public ItemNotFoundException(Guid itemId)
        : base($"Jellyfin item '{itemId}' was not found or is not accessible to the current user.")
    {
    }
}
