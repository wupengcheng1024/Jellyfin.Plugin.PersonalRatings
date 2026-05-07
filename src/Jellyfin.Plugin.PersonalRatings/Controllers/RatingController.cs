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
/// Single-item rating endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings")]
public sealed class RatingController : ControllerBase
{
    private readonly ILogger<RatingController> _logger;
    private readonly IRatingService _ratingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingController"/> class.
    /// </summary>
    /// <param name="ratingService">The rating service.</param>
    /// <param name="logger">The logger.</param>
    public RatingController(IRatingService ratingService, ILogger<RatingController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's rating for an item.
    /// </summary>
    /// <param name="itemId">The Jellyfin item id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The current rating payload.</returns>
    [HttpGet("rating")]
    [ProducesResponseType<RatingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingResponse>> GetRating([FromQuery] string itemId, CancellationToken cancellationToken)
    {
        if (!TryGetRequestContext(itemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            RatingResponse response = await _ratingService.GetRatingAsync(userId, parsedItemId, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to load rating for item {ItemId}", itemId);
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while loading rating for item {ItemId}", itemId);
            return Problem(title: "Failed to load rating.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Sets the current user's rating for an item.
    /// </summary>
    /// <param name="request">The rating request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated rating payload.</returns>
    [HttpPost("rating")]
    [ProducesResponseType<RatingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingResponse>> SetRating([FromBody] SetRatingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetRequestContext(request.ItemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            RatingResponse response = await _ratingService.SetRatingAsync(userId, parsedItemId, request.Score, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            _logger.LogWarning(exception, "Rejected score {Score} for item {ItemId}", request.Score, request.ItemId);
            return BadRequest(exception.Message);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to set rating for item {ItemId}", request.ItemId);
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while setting rating for item {ItemId}", request.ItemId);
            return Problem(title: "Failed to save rating.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Clears the current user's rating for an item.
    /// </summary>
    /// <param name="itemId">The Jellyfin item id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The cleared rating payload.</returns>
    [HttpDelete("rating")]
    [ProducesResponseType<RatingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingResponse>> ClearRating([FromQuery] string itemId, CancellationToken cancellationToken)
    {
        if (!TryGetRequestContext(itemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            RatingResponse response = await _ratingService.ClearRatingAsync(userId, parsedItemId, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to clear rating for item {ItemId}", itemId);
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while clearing rating for item {ItemId}", itemId);
            return Problem(title: "Failed to clear rating.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private bool TryGetRequestContext(
        string itemId,
        out Guid userId,
        out Guid parsedItemId,
        out ActionResult? errorResult)
    {
        if (!User.TryGetJellyfinUserId(out userId))
        {
            parsedItemId = Guid.Empty;
            errorResult = Unauthorized();
            return false;
        }

        if (!Guid.TryParse(itemId, out parsedItemId))
        {
            errorResult = BadRequest("itemId must be a valid GUID.");
            return false;
        }

        errorResult = null;
        return true;
    }
}
