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
/// Batch rating endpoints for the management page.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings/ratings/batch")]
public sealed class RatingBatchController : ControllerBase
{
    private readonly ILogger<RatingBatchController> _logger;
    private readonly IRatingService _ratingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingBatchController"/> class.
    /// </summary>
    /// <param name="ratingService">The rating service.</param>
    /// <param name="logger">The logger.</param>
    public RatingBatchController(IRatingService ratingService, ILogger<RatingBatchController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Sets one score for many items.
    /// </summary>
    /// <param name="request">The batch score request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    [HttpPost("set-score")]
    [ProducesResponseType<BatchOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BatchOperationResponse>> SetScore([FromBody] BatchSetScoreRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        if (!TryParseItemIds(request.ItemIds, out IReadOnlyList<Guid> itemIds, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            BatchOperationResponse response = await _ratingService.BatchSetRatingAsync(userId, itemIds, request.Score, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            _logger.LogWarning(exception, "Rejected batch set-score request for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected batch set-score request for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Batch set-score request contained an inaccessible item for user {UserId}", userId);
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while setting scores in batch for user {UserId}", userId);
            return Problem(title: "Failed to set scores in batch.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Clears ratings for many items.
    /// </summary>
    /// <param name="request">The batch clear request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    [HttpPost("clear-score")]
    [ProducesResponseType<BatchOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BatchOperationResponse>> ClearScore([FromBody] BatchClearRatingsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        if (!TryParseItemIds(request.ItemIds, out IReadOnlyList<Guid> itemIds, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            BatchOperationResponse response = await _ratingService.BatchClearRatingAsync(userId, itemIds, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected batch clear-score request for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Batch clear-score request contained an inaccessible item for user {UserId}", userId);
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while clearing scores in batch for user {UserId}", userId);
            return Problem(title: "Failed to clear scores in batch.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Marks many items as pending deletion.
    /// </summary>
    /// <param name="request">The pending-delete request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    [HttpPost("set-pending-delete")]
    [ProducesResponseType<BatchOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BatchOperationResponse>> SetPendingDelete([FromBody] BatchPendingDeleteRequest request, CancellationToken cancellationToken)
    {
        return await UpdatePendingDeleteAsync(request, true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the pending-delete flag from many items.
    /// </summary>
    /// <param name="request">The pending-delete request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation response.</returns>
    [HttpPost("unset-pending-delete")]
    [ProducesResponseType<BatchOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BatchOperationResponse>> UnsetPendingDelete([FromBody] BatchPendingDeleteRequest request, CancellationToken cancellationToken)
    {
        return await UpdatePendingDeleteAsync(request, false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ActionResult<BatchOperationResponse>> UpdatePendingDeleteAsync(
        BatchPendingDeleteRequest request,
        bool isPendingDelete,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        if (!TryParseItemIds(request.ItemIds, out IReadOnlyList<Guid> itemIds, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            BatchOperationResponse response = await _ratingService.BatchSetPendingDeleteAsync(userId, itemIds, isPendingDelete, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected batch pending-delete request for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Batch pending-delete request contained an inaccessible item for user {UserId}", userId);
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating pending delete in batch for user {UserId}", userId);
            return Problem(title: "Failed to update pending-delete state in batch.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        return User.TryGetJellyfinUserId(out userId);
    }

    private static bool TryParseItemIds(
        IReadOnlyList<string> rawItemIds,
        out IReadOnlyList<Guid> itemIds,
        out ActionResult? errorResult)
    {
        List<Guid> parsed = [];

        foreach (string rawItemId in rawItemIds)
        {
            if (!Guid.TryParse(rawItemId, out Guid itemId))
            {
                itemIds = Array.Empty<Guid>();
                errorResult = new BadRequestObjectResult($"itemIds contains an invalid GUID: {rawItemId}");
                return false;
            }

            parsed.Add(itemId);
        }

        itemIds = parsed;
        errorResult = null;
        return true;
    }
}
