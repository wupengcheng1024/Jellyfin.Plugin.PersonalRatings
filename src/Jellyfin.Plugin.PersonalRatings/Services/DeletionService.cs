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
    private const string ResultAuditUnavailable = "auditUnavailable";
    private const string ResultDeleteFailed = "deleteFailed";
    private const string ResultDeleted = "deleted";
    private const string ResultForbidden = "forbidden";
    private const string ResultNotFound = "notFound";
    private const string AuditStatusAttemptLogged = "attemptLogged";
    private const string AuditStatusCompleted = "completed";
    private const string AuditStatusNone = "none";
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

        bool isAdministrator = operatorUser.HasPermission(PermissionKind.IsAdministrator);
        if (!isAdministrator)
        {
            await TryWriteForbiddenAuditLogsAsync(operatorUserId, normalizedItemIds, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Rejected physical delete request from non-administrator user {UserId} for {Count} items", operatorUserId, normalizedItemIds.Count);
            throw new UnauthorizedAccessException("Physical delete requires administrator privileges.");
        }

        List<PhysicalDeleteItemResponse> items = [];
        foreach (Guid itemId in normalizedItemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PhysicalDeleteItemResponse itemResponse = await DeleteSingleItemAsync(operatorUserId, itemId, cancellationToken).ConfigureAwait(false);
            items.Add(itemResponse);
        }

        Int32 deletedCount = items.Count(item => string.Equals(item.Result, ResultDeleted, StringComparison.Ordinal));
        Int32 attentionCount = items.Count(item => !string.IsNullOrWhiteSpace(item.SuggestedAction));
        return new PhysicalDeleteResponse
        {
            RequestedCount = normalizedItemIds.Count,
            DeletedCount = deletedCount,
            FailedCount = items.Count - deletedCount,
            AttentionCount = attentionCount,
            Items = items
        };
    }

    private async Task<PhysicalDeleteItemResponse> DeleteSingleItemAsync(
        Guid operatorUserId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        JellyfinDeletionTarget? target = await _jellyfinDeletionAdapter.GetTargetAsync(itemId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return await BuildMissingItemResponseAsync(operatorUserId, itemId, cancellationToken).ConfigureAwait(false);
        }

        string preDeleteMessage = "Physical delete was confirmed by an administrator and is about to run.";
        DeleteAuditLog preDeleteAuditLog = BuildAuditLog(operatorUserId, target.ItemId, target.ItemName, "requested", preDeleteMessage);
        bool preDeleteAuditWritten = await TryWriteAuditLogAsync(preDeleteAuditLog, cancellationToken).ConfigureAwait(false);
        if (!preDeleteAuditWritten)
        {
            _logger.LogError(
                "Blocked physical delete for item {ItemId} ({ItemName}) because the required audit log could not be written",
                target.ItemId,
                target.ItemName);

            return new PhysicalDeleteItemResponse
            {
                ItemId = target.ItemId.ToString("D"),
                ItemName = target.ItemName,
                Result = ResultAuditUnavailable,
                AuditStatus = AuditStatusNone,
                Message = "Physical delete was blocked because the plugin could not persist the required audit record.",
                SuggestedAction = "检查插件数据库写权限、磁盘空间和 SQLite 可用性，然后重试。"
            };
        }

        try
        {
            await _jellyfinDeletionAdapter.DeleteAsync(target, cancellationToken).ConfigureAwait(false);

            PhysicalDeleteItemResponse response = new()
            {
                ItemId = target.ItemId.ToString("D"),
                ItemName = target.ItemName,
                Result = ResultDeleted,
                AuditStatus = AuditStatusAttemptLogged,
                Message = "The item was deleted from Jellyfin."
            };

            bool deletedAuditWritten = await TryWriteOutcomeAuditAsync(
                operatorUserId,
                target.ItemId,
                target.ItemName,
                ResultDeleted,
                "The item was deleted from Jellyfin.",
                cancellationToken).ConfigureAwait(false);

            if (deletedAuditWritten)
            {
                response.AuditStatus = AuditStatusCompleted;
            }
            else
            {
                response.Message = AppendSentence(response.Message, "The initial audit entry was saved, but the final delete audit entry could not be written.");
                response.SuggestedAction = AppendSuggestedAction(
                    response.SuggestedAction,
                    "先检查 delete_audit_logs 和 Jellyfin 服务器日志，再继续执行其他物理删除。");
            }

            await TryCleanupRatingsAsync(operatorUserId, response, target, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("User {UserId} physically deleted item {ItemId} ({ItemName})", operatorUserId, target.ItemId, target.ItemName);
            return response;
        }
        catch (Exception exception)
        {
            PhysicalDeleteItemResponse response = new()
            {
                ItemId = target.ItemId.ToString("D"),
                ItemName = target.ItemName,
                Result = ResultDeleteFailed,
                AuditStatus = AuditStatusAttemptLogged,
                Message = exception.Message,
                SuggestedAction = "检查 Jellyfin 对底层媒体路径的删除权限、文件锁状态和条目可访问性，然后重试。"
            };

            bool failedAuditWritten = await TryWriteOutcomeAuditAsync(
                operatorUserId,
                target.ItemId,
                target.ItemName,
                ResultDeleteFailed,
                exception.Message,
                cancellationToken).ConfigureAwait(false);

            if (failedAuditWritten)
            {
                response.AuditStatus = AuditStatusCompleted;
            }
            else
            {
                response.SuggestedAction = AppendSuggestedAction(
                    response.SuggestedAction,
                    "同时检查插件数据库，因为这次失败结果没有成功写入最终审计记录。");
            }

            _logger.LogError(exception, "Failed to physically delete item {ItemId} ({ItemName}) for user {UserId}", target.ItemId, target.ItemName, operatorUserId);
            return response;
        }
    }

    private async Task<PhysicalDeleteItemResponse> BuildMissingItemResponseAsync(
        Guid operatorUserId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        PhysicalDeleteItemResponse response = new()
        {
            ItemId = itemId.ToString("D"),
            Result = ResultNotFound,
            AuditStatus = AuditStatusNone,
            Message = "The item no longer exists or cannot be resolved by Jellyfin.",
            SuggestedAction = "刷新当前列表并确认该条目仍存在于 Jellyfin 中，然后再决定是否重试。"
        };

        bool auditWritten = await TryWriteOutcomeAuditAsync(
            operatorUserId,
            itemId,
            null,
            ResultNotFound,
            response.Message,
            cancellationToken).ConfigureAwait(false);

        response.AuditStatus = auditWritten ? AuditStatusCompleted : AuditStatusNone;
        if (!auditWritten)
        {
            response.SuggestedAction = AppendSuggestedAction(
                response.SuggestedAction,
                "另外检查插件数据库写入状态，因为这次未找到结果没有成功写入审计。");
        }

        _logger.LogWarning("Physical delete skipped missing item {ItemId} requested by user {UserId}", itemId, operatorUserId);
        return response;
    }

    private async Task TryCleanupRatingsAsync(
        Guid operatorUserId,
        PhysicalDeleteItemResponse response,
        JellyfinDeletionTarget target,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid[] deletedItemIds = [target.ItemId];
            Int32 deletedRatingsCount = await _ratingRepository.DeleteForItemsAsync(deletedItemIds, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} rating rows after physical delete by user {UserId} for item {ItemId}", deletedRatingsCount, operatorUserId, target.ItemId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Deleted item {ItemId} ({ItemName}) but failed to clean related rating rows", target.ItemId, target.ItemName);
            response.Message = AppendSentence(response.Message, "The media item was deleted, but rating cleanup did not complete.");
            response.SuggestedAction = AppendSuggestedAction(
                response.SuggestedAction,
                "刷新“我的评分库”，如果仍看到残留评分记录，再手动清理对应 SQLite 行。");
        }
    }

    private async Task TryWriteForbiddenAuditLogsAsync(
        Guid operatorUserId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool auditWritten = await TryWriteOutcomeAuditAsync(
                operatorUserId,
                itemId,
                null,
                ResultForbidden,
                "Physical delete requires administrator privileges.",
                cancellationToken).ConfigureAwait(false);

            if (!auditWritten)
            {
                _logger.LogError("Failed to persist forbidden delete audit log for user {UserId} and item {ItemId}", operatorUserId, itemId);
            }
        }
    }

    private async Task<bool> TryWriteOutcomeAuditAsync(
        Guid operatorUserId,
        Guid itemId,
        string? itemName,
        string result,
        string? message,
        CancellationToken cancellationToken)
    {
        DeleteAuditLog auditLog = BuildAuditLog(operatorUserId, itemId, itemName, result, message);
        return await TryWriteAuditLogAsync(auditLog, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryWriteAuditLogAsync(DeleteAuditLog auditLog, CancellationToken cancellationToken)
    {
        try
        {
            await _deleteAuditLogRepository.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist delete audit log for item {ItemId} with result {Result}",
                auditLog.ItemId,
                auditLog.Result);
            return false;
        }
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

    private static string AppendSentence(string? currentMessage, string extraSentence)
    {
        if (string.IsNullOrWhiteSpace(currentMessage))
        {
            return extraSentence;
        }

        char lastCharacter = currentMessage[currentMessage.Length - 1];
        if (lastCharacter == '.' || lastCharacter == '!' || lastCharacter == '?')
        {
            return currentMessage + " " + extraSentence;
        }

        return currentMessage + ". " + extraSentence;
    }

    private static string AppendSuggestedAction(string? currentSuggestion, string nextSuggestion)
    {
        if (string.IsNullOrWhiteSpace(currentSuggestion))
        {
            return nextSuggestion;
        }

        return currentSuggestion + " " + nextSuggestion;
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
