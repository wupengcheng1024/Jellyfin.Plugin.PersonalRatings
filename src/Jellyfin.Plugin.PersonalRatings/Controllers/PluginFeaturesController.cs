using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Jellyfin.Plugin.PersonalRatings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PersonalRatings.Controllers;

/// <summary>
/// Exposes a read-only snapshot of plugin feature switches for the current process.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("Plugins/PersonalRatings")]
public sealed class PluginFeaturesController : ControllerBase
{
    private readonly IPluginFeatureService _featureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginFeaturesController"/> class.
    /// </summary>
    /// <param name="featureService">The plugin feature service.</param>
    public PluginFeaturesController(IPluginFeatureService featureService)
    {
        _featureService = featureService;
    }

    /// <summary>
    /// Gets the current plugin feature switches.
    /// </summary>
    /// <returns>The feature-switch snapshot.</returns>
    [HttpGet("features")]
    [ProducesResponseType<PluginFeaturesResponse>(StatusCodes.Status200OK)]
    public ActionResult<PluginFeaturesResponse> GetFeatures()
    {
        return Ok(new PluginFeaturesResponse
        {
            IsDeleteFeatureEnabled = _featureService.IsDeleteFeatureEnabled,
            IsDetailsPageInjectionEnabled = _featureService.IsDetailsPageInjectionEnabled,
            IsManagePageEnabled = _featureService.IsManagePageEnabled
        });
    }
}
