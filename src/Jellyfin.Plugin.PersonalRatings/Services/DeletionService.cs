using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Services;

internal sealed class DeletionService : IDeletionService
{
    private const string DeleteOperationName = "deletePhysical";
    private readonly IDeleteAuditLogRepository _deleteAuditLogRepository;
    private readonly IJellyfinDeletionAdapter _jellyfinDeletionAdapter;
    private readonly ILogger<DeletionService> _logger;
    private readonly IRatingRepository _ratingRepository;
    private readonly IUserManager _userManager;

    public DeletionService(
        IUserManager userManager,
        IRatingRepository ratingRepository,
        IDeleteAuditLogRepository deleteAuditLogRepository,
        IJellyfinDeletionAdapter jellyfinDeletionAdapter,
        ILogger<DeletionService> logger)
    {
        _userManager = userManager;
        _ratingRepository = ratingRepository;
        _deleteAuditLogRepository = deleteAuditLogRepository;
        _jellyfinDeletionAdapter = jellyfinDeletionAdapter;
        _logger = logger;
    }

    public async Task<PhysicalDeleteResponse> DeleteItemsAsync(
        Guid operatorUserId,
        IReadOnlyList<Guid> itemIds,
        bool confirmDelete,
        CancellationToken cancellationToken)
    {
        if (!confirmDelete)
        {
            throw new ArgumentException("Physical delete requires confirmDelete=true.", nameof(confirmDelete));
        }

        IReadOnlyList<Guid> normalizedItemIds = NormalizeItemIds(itemIds);
        User? operatorUser = _userManager.GetUserById(operatorUserId);
        if (operatorUser is null)
        {
            throw new UnauthorizedAccessException("The current operator user could not be resolved.");
        }

        bool requireAdmin = Plugin.Instance?.Configuration.RequireAdminForPhysicalDelete ?? true;
        bool isAdministrator = operatorUser.HasPermission(PermissionKind.IsAdministrator);
        if (requireAdmin && !isAdministrator)
        {
            IReadOnlyList<DeleteAuditLog> forbiddenAuditLogs = normalizedItemIds
                .Select(itemId => BuildAuditLog(operatorUserId, itemId, null, "forbidden", "Physical delete requires administrator privileges."))
                .ToList();

            await _deleteAuditLogRepository.AddRangeAsync(forbiddenAuditLogs, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Rejected physical delete request from non-administrator user {UserId} for {Count} items", operatorUserId, normalizedItemIds.Count);
            throw new UnauthorizedAccessException("Physical delete requires administrator privileges.");
        }

        List<PhysicalDeleteItemResponse> items = [];
        List<DeleteAuditLog> auditLogs = [];
        List<Guid> deletedItemIds = [];

        foreach (Guid itemId in normalizedItemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            JellyfinDeletionTarget? target = await _jellyfinDeletionAdapter.GetTargetAsync(itemId, cancellationToken).ConfigureAwait(false);
            if (target is null)
            {
                items.Add(new PhysicalDeleteItemResponse
                {
                    ItemId = itemId.ToString("D"),
                    Result = "notFound",
                    Message = "The item no longer exists or cannot be resolved by Jellyfin."
                });
                auditLogs.Add(BuildAuditLog(operatorUserId, itemId, null, "notFound", "The item no longer exists or cannot be resolved by Jellyfin."));
                _logger.LogWarning("Physical delete skipped missing item {ItemId} requested by user {UserId}", itemId, operatorUserId);
                continue;
            }

            try
            {
                await _jellyfinDeletionAdapter.DeleteAsync(target, cancellationToken).ConfigureAwait(false);
                deletedItemIds.Add(target.ItemId);
                items.Add(new PhysicalDeleteItemResponse
                {
                    ItemId = target.ItemId.ToString("D"),
                    ItemName = target.ItemName,
                    Result = "deleted",
                    Message = "The item was deleted from Jellyfin."
                });
                auditLogs.Add(BuildAuditLog(operatorUserId, target.ItemId, target.ItemName, "deleted", "The item was deleted from Jellyfin."));
                _logger.LogInformation("User {UserId} physically deleted item {ItemId} ({ItemName})", operatorUserId, target.ItemId, target.ItemName);
            }
            catch (Exception exception)
            {
                items.Add(new PhysicalDeleteItemResponse
                {
                    ItemId = target.ItemId.ToString("D"),
                    ItemName = target.ItemName,
                    Result = "failed",
                    Message = exception.Message
                });
                auditLogs.Add(BuildAuditLog(operatorUserId, target.ItemId, target.ItemName, "failed", exception.Message));
                _logger.LogError(exception, "Failed to physically delete item {ItemId} ({ItemName}) for user {UserId}", target.ItemId, target.ItemName, operatorUserId);
            }
        }

        if (deletedItemIds.Count > 0)
        {
            Int32 deletedRatingsCount = await _ratingRepository.DeleteForItemsAsync(deletedItemIds, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} rating rows after physical delete by user {UserId}", deletedRatingsCount, operatorUserId);
        }

        await _deleteAuditLogRepository.AddRangeAsync(auditLogs, cancellationToken).ConfigureAwait(false);

        Int32 deletedCount = items.Count(item => string.Equals(item.Result, "deleted", StringComparison.Ordinal));
        return new PhysicalDeleteResponse
        {
            RequestedCount = normalizedItemIds.Count,
            DeletedCount = deletedCount,
            FailedCount = items.Count - deletedCount,
            Items = items
        };
    }

    private static DeleteAuditLog BuildAuditLog(Guid operatorUserId, Guid itemId, string? itemName, string result, string? message)
    {
        return new DeleteAuditLog
        {
            OperatorUserId = operatorUserId,
            ItemId = itemId,
            ItemName = itemName,
            Action = DeleteOperationName,
            Result = result,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<Guid> NormalizeItemIds(IReadOnlyList<Guid> itemIds)
    {
        if (itemIds.Count == 0)
        {
            throw new ArgumentException("At least one itemId is required.", nameof(itemIds));
        }

        List<Guid> normalizedItemIds = [];
        HashSet<Guid> seen = [];

        foreach (Guid itemId in itemIds)
        {
            if (seen.Add(itemId))
            {
                normalizedItemIds.Add(itemId);
            }
        }

        return normalizedItemIds;
    }
}
