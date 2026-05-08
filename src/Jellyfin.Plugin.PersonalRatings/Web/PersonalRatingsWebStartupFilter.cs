using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.PersonalRatings.Web;

/// <summary>
/// Inserts the HTML injection middleware into Jellyfin Web requests.
/// </summary>
public sealed class PersonalRatingsWebStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return applicationBuilder =>
        {
            applicationBuilder.UseMiddleware<PersonalRatingsHtmlInjectionMiddleware>();
            next(applicationBuilder);
        };
    }
}
