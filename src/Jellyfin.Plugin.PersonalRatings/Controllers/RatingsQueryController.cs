using Jellyfin.Plugin.PersonalRatings.Infrastructure;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Jellyfin.Plugin.PersonalRatings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Controllers;

/// <summary>
/// Ratings list query endpoints for the management page.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings")]
public sealed class RatingsQueryController : ControllerBase
{
    private readonly ILogger<RatingsQueryController> _logger;
    private readonly IRatingService _ratingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingsQueryController"/> class.
    /// </summary>
    /// <param name="ratingService">The rating service.</param>
    /// <param name="logger">The logger.</param>
    public RatingsQueryController(IRatingService ratingService, ILogger<RatingsQueryController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Queries the current user's ratings with pagination.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A paginated ratings query response.</returns>
    [HttpPost("ratings/query")]
    [ProducesResponseType<RatingQueryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RatingQueryResponse>> QueryRatings([FromBody] RatingQueryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetJellyfinUserId(out Guid userId))
        {
            return Unauthorized();
        }

        try
        {
            RatingQueryResponse response = await _ratingService.QueryRatingsAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            _logger.LogWarning(exception, "Rejected ratings query for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while querying ratings for user {UserId}", userId);
            return Problem(title: "Failed to query ratings.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
