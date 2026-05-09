using Jellyfin.Plugin.PersonalRatings.Data;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal interface IDeleteAuditLogRepository
{
    Task AddAsync(DeleteAuditLog auditLog, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyList<DeleteAuditLog> auditLogs, CancellationToken cancellationToken);

    Task<PagedQueryResult<DeleteAuditLog>> QueryPageAsync(AuditLogQueryRequest request, CancellationToken cancellationToken);
}
