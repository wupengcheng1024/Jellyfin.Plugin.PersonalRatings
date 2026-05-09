using System.Globalization;
using System.IO;
using System.Reflection;
using Jellyfin.Plugin.PersonalRatings.Services;
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
    private readonly IPluginFeatureService _featureService;
    private readonly ILogger<WebAssetsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAssetsController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WebAssetsController(IPluginFeatureService featureService, ILogger<WebAssetsController> logger)
    {
        _featureService = featureService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the injected details-page rating script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("details-rating.js")]
    public ActionResult GetDetailsRatingScript()
    {
        if (!_featureService.IsDetailsPageInjectionEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("details-rating.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the detail-page API helper script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("details-api.js")]
    public ActionResult GetDetailsApiScript()
    {
        if (!_featureService.IsDetailsPageInjectionEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("details-api.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the detail-page panel rendering script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("details-panel.js")]
    public ActionResult GetDetailsPanelScript()
    {
        if (!_featureService.IsDetailsPageInjectionEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("details-panel.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the front-end browse-page state script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("browse-state.js")]
    public ActionResult GetBrowseStateScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-state.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the front-end browse-page API helper script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("browse-api.js")]
    public ActionResult GetBrowseApiScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-api.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the front-end browse-page renderer script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("browse-render.js")]
    public ActionResult GetBrowseRenderScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-render.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the front-end browse-page toolbar / filter script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("browse-filters.js")]
    public ActionResult GetBrowseFiltersScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-filters.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the injected front-end browse-page shell script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("browse-shell.js")]
    public ActionResult GetBrowseShellScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-shell.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the front-end browse-page stylesheet.
    /// </summary>
    /// <returns>The stylesheet asset.</returns>
    [HttpGet("browse-page.css")]
    public ActionResult GetBrowsePageStyles()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("browse-page.css", "text/css; charset=utf-8");
    }

    /// <summary>
    /// Gets the management page script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("manage-page.js")]
    public ActionResult GetManagePageScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("manage-page.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the management page stylesheet.
    /// </summary>
    /// <returns>The stylesheet asset.</returns>
    [HttpGet("manage-page.css")]
    public ActionResult GetManagePageStyles()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("manage-page.css", "text/css; charset=utf-8");
    }

    /// <summary>
    /// Gets the audit page script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("audit-page.js")]
    public ActionResult GetAuditPageScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("audit-page.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the tag-management page script.
    /// </summary>
    /// <returns>The JavaScript asset.</returns>
    [HttpGet("tag-manage-page.js")]
    public ActionResult GetTagManagePageScript()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("tag-manage-page.js", "text/javascript; charset=utf-8");
    }

    /// <summary>
    /// Gets the tag-management page stylesheet.
    /// </summary>
    /// <returns>The stylesheet asset.</returns>
    [HttpGet("tag-manage-page.css")]
    public ActionResult GetTagManagePageStyles()
    {
        if (!_featureService.IsManagePageEnabled)
        {
            return NotFound();
        }

        return GetEmbeddedAsset("tag-manage-page.css", "text/css; charset=utf-8");
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
