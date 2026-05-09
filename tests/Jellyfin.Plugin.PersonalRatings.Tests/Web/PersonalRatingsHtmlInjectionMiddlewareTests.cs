using System.Text;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Jellyfin.Plugin.PersonalRatings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Web;

public sealed class PersonalRatingsHtmlInjectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DoesNotInjectAnyPluginScripts_WhenBothFrontEntryAndDetailsInjectionAreDisabled()
    {
        RequestDelegate next = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<html><body><div>hello</div></body></html>");
        };

        PersonalRatingsHtmlInjectionMiddleware middleware = new(
            next,
            new TestFeatureService
            {
                IsDetailsPageInjectionEnabled = false,
                IsManagePageEnabled = false
            },
            NullLogger<PersonalRatingsHtmlInjectionMiddleware>.Instance);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/web/index.html";
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await middleware.InvokeAsync(httpContext);

        string html = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.DoesNotContain("/Plugins/PersonalRatings/web/details-rating.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/Plugins/PersonalRatings/web/browse-shell.js", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_InjectsBrowseShell_WhenFrontEntryIsEnabled()
    {
        RequestDelegate next = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<html><body><div>hello</div></body></html>");
        };

        PersonalRatingsHtmlInjectionMiddleware middleware = new(
            next,
            new TestFeatureService
            {
                IsDetailsPageInjectionEnabled = false,
                IsManagePageEnabled = true
            },
            NullLogger<PersonalRatingsHtmlInjectionMiddleware>.Instance);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/web/index.html";
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await middleware.InvokeAsync(httpContext);

        string html = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("/Plugins/PersonalRatings/web/browse-shell.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/Plugins/PersonalRatings/web/details-rating.js", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_InjectsScript_WhenDetailsInjectionIsEnabled()
    {
        RequestDelegate next = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<html><body><div>hello</div></body></html>");
        };

        PersonalRatingsHtmlInjectionMiddleware middleware = new(
            next,
            new TestFeatureService
            {
                IsDetailsPageInjectionEnabled = true
            },
            NullLogger<PersonalRatingsHtmlInjectionMiddleware>.Instance);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/web/index.html";
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await middleware.InvokeAsync(httpContext);

        string html = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("/Plugins/PersonalRatings/web/details-rating.js", html, StringComparison.Ordinal);
        Assert.Contains("/Plugins/PersonalRatings/web/browse-shell.js", html, StringComparison.Ordinal);
    }
}
