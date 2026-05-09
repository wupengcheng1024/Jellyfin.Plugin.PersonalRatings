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
/// Delete audit-log query endpoints for administrators.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/PersonalRatings")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogsController"/> class.
    /// </summary>
    /// <param name="auditLogService">The audit-log service.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Queries delete audit logs with pagination.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A paginated audit-log query response.</returns>
    [HttpPost("audit-logs/query")]
    [ProducesResponseType<AuditLogQueryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogQueryResponse>> QueryAuditLogs([FromBody] AuditLogQueryRequest request, CancellationToken cancellationToken)
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
            AuditLogQueryResponse response = await _auditLogService.QueryAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Rejected delete audit-log query for user {UserId}", userId);
            return BadRequest(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Rejected delete audit-log query for user {UserId}", userId);
            return Forbid();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while querying delete audit logs for user {UserId}", userId);
            return Problem(title: "Failed to query delete audit logs.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
