using Jellyfin.Plugin.PersonalRatings.Models.Entities;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal interface IDeleteAuditLogRepository
{
    Task AddRangeAsync(IReadOnlyList<DeleteAuditLog> auditLogs, CancellationToken cancellationToken);
}
