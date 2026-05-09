using System.Security.Claims;
using Jellyfin.Plugin.PersonalRatings.Infrastructure;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Helpers;

public static class TestClaimsPrincipalFactory
{
    public static ClaimsPrincipal CreateAuthenticatedUser(Guid userId)
    {
        Claim[] claims =
        [
            new Claim(JellyfinClaimTypes.UserId, userId.ToString("D"))
        ];

        ClaimsIdentity identity = new(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
