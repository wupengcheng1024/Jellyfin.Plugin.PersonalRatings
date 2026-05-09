using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalRatings.Data;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class AuditLogService : IAuditLogService
{
    private readonly IDeleteAuditLogRepository _deleteAuditLogRepository;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IUserManager _userManager;

    public AuditLogService(
        IUserManager userManager,
        IDeleteAuditLogRepository deleteAuditLogRepository,
        ILogger<AuditLogService> logger)
    {
        _userManager = userManager;
        _deleteAuditLogRepository = deleteAuditLogRepository;
        _logger = logger;
    }

    public async Task<AuditLogQueryResponse> QueryAsync(Guid operatorUserId, AuditLogQueryRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        User? operatorUser = _userManager.GetUserById(operatorUserId);
        if (operatorUser is null)
        {
            throw new UnauthorizedAccessException("The current operator user could not be resolved.");
        }

        if (!operatorUser.HasPermission(PermissionKind.IsAdministrator))
        {
            _logger.LogWarning("Rejected delete audit-log query from non-administrator user {UserId}", operatorUserId);
            throw new UnauthorizedAccessException("Delete audit logs require administrator privileges.");
        }

        Data.PagedQueryResult<DeleteAuditLog> pagedRows = await _deleteAuditLogRepository
            .QueryPageAsync(request, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AuditLogListItemResponse> items = pagedRows.Items
            .Select(MapItem)
            .ToList();

        return new AuditLogQueryResponse
        {
            Items = items,
            TotalCount = pagedRows.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static AuditLogListItemResponse MapItem(DeleteAuditLog auditLog)
    {
        return new AuditLogListItemResponse
        {
            Id = auditLog.Id,
            OperatorUserId = auditLog.OperatorUserId.ToString("D"),
            ItemId = auditLog.ItemId.ToString("D"),
            ItemName = auditLog.ItemName,
            Action = auditLog.Action,
            Result = auditLog.Result,
            Message = auditLog.Message,
            CreatedAt = auditLog.CreatedAt
        };
    }

    private static void ValidateRequest(AuditLogQueryRequest request)
    {
        if (request.CreatedAfterUtc.HasValue
            && request.CreatedBeforeUtc.HasValue
            && request.CreatedAfterUtc.Value > request.CreatedBeforeUtc.Value)
        {
            throw new ArgumentException("createdAfterUtc must be earlier than or equal to createdBeforeUtc.");
        }

        if (!string.IsNullOrWhiteSpace(request.ItemId) && !Guid.TryParse(request.ItemId, out _))
        {
            throw new ArgumentException("itemId must be a valid GUID when provided.");
        }
    }
}
