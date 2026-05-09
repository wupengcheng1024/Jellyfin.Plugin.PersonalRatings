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
/// Tag-definition endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings")]
public sealed class TagsController : ControllerBase
{
    private readonly ILogger<TagsController> _logger;
    private readonly ITagService _tagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagsController"/> class.
    /// </summary>
    /// <param name="tagService">The tag service.</param>
    /// <param name="logger">The logger.</param>
    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    /// <summary>
    /// Lists tag definitions.
    /// </summary>
    /// <param name="includeDisabled">Whether disabled tags should be included.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The tag definitions.</returns>
    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<TagDefinitionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TagDefinitionResponse>>> GetTags(
        [FromQuery] bool includeDisabled,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetJellyfinUserId(out Guid userId))
        {
            return Unauthorized();
        }

        try
        {
            IReadOnlyList<TagDefinitionResponse> response = await _tagService
                .GetTagDefinitionsAsync(userId, includeDisabled, cancellationToken)
                .ConfigureAwait(false);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Rejected tag query with includeDisabled={IncludeDisabled} for user {UserId}", includeDisabled, userId);
            return Forbid();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while querying tag definitions for user {UserId}", userId);
            return Problem(title: "Failed to query tag definitions.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Creates one tag definition.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created tag definition.</returns>
    [HttpPost("tags")]
    [ProducesResponseType<TagDefinitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagDefinitionResponse>> CreateTag(
        [FromBody] CreateTagDefinitionRequest request,
        CancellationToken cancellationToken)
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
            TagDefinitionResponse response = await _tagService.CreateTagDefinitionAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Rejected tag creation for user {UserId}", userId);
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected tag creation for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a tag definition for user {UserId}", userId);
            return Problem(title: "Failed to create tag definition.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates one tag definition.
    /// </summary>
    /// <param name="id">The target tag id.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The updated tag definition.</returns>
    [HttpPut("tags/{id:long}")]
    [ProducesResponseType<TagDefinitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDefinitionResponse>> UpdateTag(
        [FromRoute] long id,
        [FromBody] UpdateTagDefinitionRequest request,
        CancellationToken cancellationToken)
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
            TagDefinitionResponse response = await _tagService.UpdateTagDefinitionAsync(userId, id, request, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Rejected tag update for user {UserId}", userId);
            return Forbid();
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to update missing tag definition {TagId}", id);
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected tag update for tag {TagId}", id);
            return BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating tag definition {TagId}", id);
            return Problem(title: "Failed to update tag definition.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes one tag definition.
    /// </summary>
    /// <param name="id">The target tag id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the tag is deleted.</returns>
    [HttpDelete("tags/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTag([FromRoute] long id, CancellationToken cancellationToken)
    {
        if (!User.TryGetJellyfinUserId(out Guid userId))
        {
            return Unauthorized();
        }

        try
        {
            await _tagService.DeleteTagDefinitionAsync(userId, id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Rejected tag deletion for user {UserId}", userId);
            return Forbid();
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Unable to delete missing tag definition {TagId}", id);
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deleting tag definition {TagId}", id);
            return Problem(title: "Failed to delete tag definition.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
