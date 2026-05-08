using Jellyfin.Plugin.PersonalRatings.Models.Entities;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal interface IDeleteAuditLogRepository
{
    Task AddAsync(DeleteAuditLog auditLog, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyList<DeleteAuditLog> auditLogs, CancellationToken cancellationToken);
}
