using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;

namespace Jellyfin.Plugin.PersonalRatings.Services;

/// <summary>
/// Business service for administrator-only delete audit-log queries.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Queries delete audit logs for an administrator.
    /// </summary>
    /// <param name="operatorUserId">The Jellyfin operator user id.</param>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated audit-log query response.</returns>
    Task<AuditLogQueryResponse> QueryAsync(Guid operatorUserId, AuditLogQueryRequest request, CancellationToken cancellationToken);
}
