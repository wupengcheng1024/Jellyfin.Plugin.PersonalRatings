using System.Security.Claims;

namespace Jellyfin.Plugin.PersonalRatings.Infrastructure;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetJellyfinUserId(this ClaimsPrincipal claimsPrincipal, out Guid userId)
    {
        string? value = claimsPrincipal.FindFirst(JellyfinClaimTypes.UserId)?.Value;
        return Guid.TryParse(value, out userId);
    }
}
