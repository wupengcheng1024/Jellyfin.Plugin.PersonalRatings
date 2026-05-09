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
/// Current-user item-tag endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings")]
public sealed class ItemTagsController : ControllerBase
{
    private readonly ILogger<ItemTagsController> _logger;
    private readonly ITagService _tagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemTagsController"/> class.
    /// </summary>
    /// <param name="tagService">The tag service.</param>
    /// <param name="logger">The logger.</param>
    public ItemTagsController(ITagService tagService, ILogger<ItemTagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's tags for one item.
    /// </summary>
    /// <param name="itemId">The target Jellyfin item id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The assigned tags.</returns>
    [HttpGet("item-tags")]
    [ProducesResponseType<ItemTagsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemTagsResponse>> GetItemTags([FromQuery] string itemId, CancellationToken cancellationToken)
    {
        if (!TryGetContext(itemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            ItemTagsResponse response = await _tagService.GetItemTagsAsync(userId, parsedItemId, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to load item tags for item {ItemId}", itemId);
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while loading item tags for item {ItemId}", itemId);
            return Problem(title: "Failed to load item tags.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Replaces the current user's tags for one item.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated assigned tags.</returns>
    [HttpPut("item-tags")]
    [ProducesResponseType<ItemTagsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemTagsResponse>> PutItemTags([FromBody] UpdateItemTagsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetContext(request.ItemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult))
        {
            return errorResult!;
        }

        try
        {
            ItemTagsResponse response = await _tagService
                .ReplaceItemTagsAsync(userId, parsedItemId, request.TagIds, cancellationToken)
                .ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected item tag update for item {ItemId}", request.ItemId);
            return BadRequest(exception.Message);
        }
        catch (ItemNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to update item tags for item {ItemId}", request.ItemId);
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while replacing item tags for item {ItemId}", request.ItemId);
            return Problem(title: "Failed to update item tags.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private bool TryGetContext(string itemId, out Guid userId, out Guid parsedItemId, out ActionResult? errorResult)
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
