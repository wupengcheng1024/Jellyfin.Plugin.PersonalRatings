using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PersonalRatings.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Web;

/// <summary>
/// Injects the personal-ratings script into Jellyfin Web shell responses.
/// </summary>
public sealed class PersonalRatingsHtmlInjectionMiddleware
{
    private readonly IPluginFeatureService _featureService;
    private readonly ILogger<PersonalRatingsHtmlInjectionMiddleware> _logger;
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalRatingsHtmlInjectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    /// <param name="logger">The logger.</param>
    public PersonalRatingsHtmlInjectionMiddleware(
        RequestDelegate next,
        IPluginFeatureService featureService,
        ILogger<PersonalRatingsHtmlInjectionMiddleware> logger)
    {
        _next = next;
        _featureService = featureService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware for the current request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A task that completes when the response is written.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!_featureService.IsDetailsPageInjectionEnabled && !_featureService.IsManagePageEnabled)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

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

            string? contentEncoding = GetContentEncoding(httpContext.Response);
            byte[] responseBytes = responseBuffer.ToArray();
            string? html = await DecodeHtmlAsync(responseBytes, contentEncoding, httpContext.RequestAborted).ConfigureAwait(false);
            if (html is null)
            {
                _logger.LogWarning(
                    "Skipping personal ratings HTML injection for {Path} because response encoding {Encoding} is not supported.",
                    httpContext.Request.Path,
                    contentEncoding ?? "(none)");

                await originalBody.WriteAsync(responseBytes, httpContext.RequestAborted).ConfigureAwait(false);
                return;
            }

            string updatedHtml = InjectScripts(html, BuildScriptTags());
            byte[] encodedHtml = await EncodeHtmlAsync(updatedHtml, contentEncoding, httpContext.RequestAborted).ConfigureAwait(false);

            RemoveStaleStaticFileHeaders(httpContext.Response);
            httpContext.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            httpContext.Response.Headers.Pragma = "no-cache";
            httpContext.Response.Headers.Expires = "0";
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

        string? contentType = response.ContentType;
        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string InjectScripts(string html, IReadOnlyList<string> scriptTags)
    {
        if (scriptTags.Count == 0)
        {
            return html;
        }

        string injectedHtml = html;
        foreach (string scriptTag in scriptTags)
        {
            if (injectedHtml.Contains(scriptTag, StringComparison.Ordinal))
            {
                continue;
            }

            int bodyIndex = injectedHtml.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyIndex >= 0)
            {
                injectedHtml = injectedHtml.Insert(bodyIndex, scriptTag);
                continue;
            }

            int headIndex = injectedHtml.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headIndex >= 0)
            {
                injectedHtml = injectedHtml.Insert(headIndex, scriptTag);
                continue;
            }

            injectedHtml += scriptTag;
        }

        return injectedHtml;
    }

    private IReadOnlyList<string> BuildScriptTags()
    {
        List<string> scriptTags = [];
        string versionToken = Plugin.Instance?.WebAssetVersionToken ?? "0";

        if (_featureService.IsManagePageEnabled)
        {
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/browse-state.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/browse-api.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/browse-render.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/browse-filters.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/browse-shell.js?v=", versionToken));
        }

        if (_featureService.IsDetailsPageInjectionEnabled)
        {
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/details-api.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/details-panel.js?v=", versionToken));
            scriptTags.Add(BuildScriptTag("/Plugins/PersonalRatings/web/details-rating.js?v=", versionToken));
        }

        return scriptTags;
    }

    private static string BuildScriptTag(string pathPrefix, string versionToken)
    {
        return string.Create(
            40 + pathPrefix.Length + versionToken.Length,
            (PathPrefix: pathPrefix, VersionToken: versionToken),
            static (buffer, state) =>
            {
                string prefix = "<script defer src=\"";
                string suffix = "\"></script>";

                prefix.AsSpan().CopyTo(buffer);
                state.PathPrefix.AsSpan().CopyTo(buffer[prefix.Length..]);
                state.VersionToken.AsSpan().CopyTo(buffer[(prefix.Length + state.PathPrefix.Length)..]);
                suffix.AsSpan().CopyTo(buffer[(prefix.Length + state.PathPrefix.Length + state.VersionToken.Length)..]);
            });
    }

    private static string? GetContentEncoding(HttpResponse response)
    {
        string? headerValue = response.Headers.ContentEncoding.ToString();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        string normalizedEncoding = headerValue.Trim();
        if (normalizedEncoding.Contains(',', StringComparison.Ordinal))
        {
            return null;
        }

        return normalizedEncoding.ToLowerInvariant();
    }

    private static async Task<string?> DecodeHtmlAsync(byte[] responseBytes, string? contentEncoding, CancellationToken cancellationToken)
    {
        await using MemoryStream responseStream = new MemoryStream(responseBytes, writable: false);
        Stream contentStream;
        if (string.IsNullOrEmpty(contentEncoding))
        {
            contentStream = responseStream;
        }
        else if (string.Equals(contentEncoding, "br", StringComparison.Ordinal))
        {
            contentStream = new BrotliStream(responseStream, CompressionMode.Decompress, leaveOpen: false);
        }
        else if (string.Equals(contentEncoding, "gzip", StringComparison.Ordinal))
        {
            contentStream = new GZipStream(responseStream, CompressionMode.Decompress, leaveOpen: false);
        }
        else
        {
            return null;
        }

        await using (contentStream.ConfigureAwait(false))
        {
            using StreamReader reader = new StreamReader(contentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<byte[]> EncodeHtmlAsync(string html, string? contentEncoding, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(contentEncoding))
        {
            return Encoding.UTF8.GetBytes(html);
        }

        byte[] uncompressedHtml = Encoding.UTF8.GetBytes(html);
        await using MemoryStream outputStream = new MemoryStream();
        Stream contentStream;

        if (string.Equals(contentEncoding, "br", StringComparison.Ordinal))
        {
            contentStream = new BrotliStream(outputStream, CompressionLevel.Fastest, leaveOpen: true);
        }
        else if (string.Equals(contentEncoding, "gzip", StringComparison.Ordinal))
        {
            contentStream = new GZipStream(outputStream, CompressionLevel.Fastest, leaveOpen: true);
        }
        else
        {
            return uncompressedHtml;
        }

        await using (contentStream.ConfigureAwait(false))
        {
            await contentStream.WriteAsync(uncompressedHtml, cancellationToken).ConfigureAwait(false);
        }

        return outputStream.ToArray();
    }

    private static void RemoveStaleStaticFileHeaders(HttpResponse response)
    {
        response.Headers.Remove("ETag");
        response.Headers.Remove("Last-Modified");
        response.Headers.Remove("Accept-Ranges");
    }
}
