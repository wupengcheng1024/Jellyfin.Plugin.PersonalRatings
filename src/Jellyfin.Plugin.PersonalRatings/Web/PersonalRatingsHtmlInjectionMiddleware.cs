using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Web;

/// <summary>
/// Injects the personal-ratings script into Jellyfin Web shell responses.
/// </summary>
public sealed class PersonalRatingsHtmlInjectionMiddleware
{
    private const string ScriptTag = "<script defer src=\"/Plugins/PersonalRatings/web/details-rating.js\"></script>";
    private readonly ILogger<PersonalRatingsHtmlInjectionMiddleware> _logger;
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalRatingsHtmlInjectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    /// <param name="logger">The logger.</param>
    public PersonalRatingsHtmlInjectionMiddleware(
        RequestDelegate next,
        ILogger<PersonalRatingsHtmlInjectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware for the current request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A task that completes when the response is written.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!ShouldInspectRequest(httpContext.Request.Path))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        Stream originalBody = httpContext.Response.Body;
        await using MemoryStream responseBuffer = new MemoryStream();
        httpContext.Response.Body = responseBuffer;

        try
        {
            await _next(httpContext).ConfigureAwait(false);

            responseBuffer.Position = 0;

            if (!ShouldInjectResponse(httpContext.Response))
            {
                await responseBuffer.CopyToAsync(originalBody, httpContext.RequestAborted).ConfigureAwait(false);
                return;
            }

            using StreamReader reader = new StreamReader(responseBuffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            string html = await reader.ReadToEndAsync(httpContext.RequestAborted).ConfigureAwait(false);
            string updatedHtml = InjectScript(html);

            byte[] encodedHtml = Encoding.UTF8.GetBytes(updatedHtml);
            httpContext.Response.ContentLength = encodedHtml.Length;
            httpContext.Response.Body = originalBody;
            await httpContext.Response.Body.WriteAsync(encodedHtml, httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to inject the personal ratings script into Jellyfin Web.");
            httpContext.Response.Body = originalBody;

            if (responseBuffer.Length > 0)
            {
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalBody, httpContext.RequestAborted).ConfigureAwait(false);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            httpContext.Response.Body = originalBody;
        }
    }

    private static bool ShouldInspectRequest(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        string value = path.Value ?? string.Empty;
        return value.Equals("/", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/web", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/web/", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/web/index.html", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldInjectResponse(HttpResponse response)
    {
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            return false;
        }

        if (response.Headers.ContainsKey("Content-Encoding"))
        {
            return false;
        }

        string? contentType = response.ContentType;
        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string InjectScript(string html)
    {
        if (html.Contains(ScriptTag, StringComparison.Ordinal))
        {
            return html;
        }

        int bodyIndex = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyIndex >= 0)
        {
            return html.Insert(bodyIndex, ScriptTag);
        }

        int headIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headIndex >= 0)
        {
            return html.Insert(headIndex, ScriptTag);
        }

        return html + ScriptTag;
    }
}
