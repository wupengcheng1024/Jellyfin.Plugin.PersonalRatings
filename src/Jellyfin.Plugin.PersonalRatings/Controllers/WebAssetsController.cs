using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Controllers;

/// <summary>
/// Serves embedded web assets used by the Jellyfin Web integration layer.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("Plugins/PersonalRatings/web")]
public sealed class WebAssetsController : ControllerBase
{
    private readonly ILogger<WebAssetsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAssetsController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WebAssetsController(ILogger<WebAssetsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the injected details-page rating script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("details-rating.js")]
    public ActionResult GetDetailsRatingScript()
    {
        return GetEmbeddedAsset("details-rating.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the management page script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("manage-page.js")]
    public ActionResult GetManagePageScript()
    {
        return GetEmbeddedAsset("manage-page.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the management page stylesheet.
    /// </summary>
    /// <returns>The stylesheet asset.</returns>
    [HttpGet("manage-page.css")]
    public ActionResult GetManagePageStyles()
    {
        return GetEmbeddedAsset("manage-page.css", "text/css; charset=utf-8");
    }

    private ActionResult GetEmbeddedAsset(string fileName, string contentType)
    {
        Assembly assembly = typeof(Plugin).Assembly;
        string resourcePath = string.Format(
            CultureInfo.InvariantCulture,
            "{0}.Web.{1}",
            typeof(Plugin).Namespace,
            fileName);

        Stream? stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream is null)
        {
            _logger.LogError("Failed to resolve embedded web asset {ResourcePath}", resourcePath);
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return File(stream, contentType);
    }
}
