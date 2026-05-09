using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Services;

public sealed class DeletionServiceTests
{
    [Fact]
    public async Task DeleteItemsAsync_ThrowsArgumentException_WhenConfirmDeleteIsFalse()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DeleteItemsAsync(operatorUserId, [itemId], false, CancellationToken.None));

        Assert.Contains("confirmDelete=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteItemsAsync_ThrowsFeatureDisabledException_WhenDeleteFeatureIsDisabled()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        TestFeatureService featureService = new()
        {
            IsDeleteFeatureEnabled = false
        };
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        FeatureDisabledException exception = await Assert.ThrowsAsync<FeatureDisabledException>(() =>
            service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None));

        Assert.Contains("disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task DeleteItemsAsync_ThrowsUnauthorizedAccessExceptionAndWritesForbiddenAudit_WhenOperatorIsNotAdministrator()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: false);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "forbidden"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None));

        auditRepository.VerifyAll();
        Assert.True(logger.Contains(LogLevel.Warning, "Rejected physical delete request"));
    }

    [Fact]
    public async Task DeleteItemsAsync_StillRejectsNonAdministrator_WhenLegacyRequireAdminFlagIsFalse()
    {
        TestPluginFactory.Create(configuration =>
        {
            configuration.EnableDeleteFeature = true;
            configuration.RequireAdminForPhysicalDelete = false;
        });

        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: false);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "forbidden"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        PluginFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None));

        Assert.True(logger.Contains(LogLevel.Warning, "Rejected physical delete request"));
    }

    [Fact]
    public async Task DeleteItemsAsync_ReturnsNotFoundResult_WhenItemDoesNotExist()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "notFound"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        deletionAdapter
            .Setup(adapter => adapter.GetTargetAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JellyfinDeletionTarget?)null);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        Models.Responses.PhysicalDeleteResponse response = await service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None);

        Assert.Equal(0, response.DeletedCount);
        Assert.Equal(1, response.FailedCount);
        Assert.Equal(1, response.AttentionCount);
        Assert.Single(response.Items);
        Assert.Equal("notFound", response.Items[0].Result);
        Assert.Equal("completed", response.Items[0].AuditStatus);
        Assert.Contains("刷新当前列表", response.Items[0].SuggestedAction, StringComparison.Ordinal);
        Assert.True(logger.Contains(LogLevel.Warning, "Physical delete skipped missing item"));
        deletionAdapter.VerifyAll();
        auditRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteItemsAsync_ReturnsAuditUnavailable_WhenPreDeleteAuditCannotBePersisted()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        JellyfinDeletionTarget target = CreateTarget(itemId, "Audit Blocked Item");
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "requested"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("disk full"));
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        deletionAdapter
            .Setup(adapter => adapter.GetTargetAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        Models.Responses.PhysicalDeleteResponse response = await service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal("auditUnavailable", response.Items[0].Result);
        Assert.Equal("none", response.Items[0].AuditStatus);
        Assert.Contains("SQLite", response.Items[0].SuggestedAction, StringComparison.Ordinal);
        deletionAdapter.Verify(adapter => adapter.DeleteAsync(It.IsAny<JellyfinDeletionTarget>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(logger.Contains(LogLevel.Error, "Blocked physical delete for item"));
    }

    [Fact]
    public async Task DeleteItemsAsync_ReturnsDeleteFailed_WhenUnderlyingDeleteThrows()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        JellyfinDeletionTarget target = CreateTarget(itemId, "Broken Item");
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "requested"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "deleteFailed" && log.Message == "boom"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        deletionAdapter
            .Setup(adapter => adapter.GetTargetAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        deletionAdapter
            .Setup(adapter => adapter.DeleteAsync(target, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        Models.Responses.PhysicalDeleteResponse response = await service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal("deleteFailed", response.Items[0].Result);
        Assert.Equal("completed", response.Items[0].AuditStatus);
        Assert.Equal("boom", response.Items[0].Message);
        Assert.Contains("删除权限", response.Items[0].SuggestedAction, StringComparison.Ordinal);
        ratingRepository.Verify(repository => repository.DeleteForItemsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(logger.Contains(LogLevel.Error, "Failed to physically delete item"));
    }

    [Fact]
    public async Task DeleteItemsAsync_ReturnsDeletedAndCleansRatings_WhenDeleteSucceeds()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        JellyfinDeletionTarget target = CreateTarget(itemId, "Deleted Item");
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteForItemsAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "requested"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "deleted"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        deletionAdapter
            .Setup(adapter => adapter.GetTargetAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        deletionAdapter
            .Setup(adapter => adapter.DeleteAsync(target, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        Models.Responses.PhysicalDeleteResponse response = await service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None);

        Assert.Equal(1, response.DeletedCount);
        Assert.Equal(0, response.FailedCount);
        Assert.Equal(0, response.AttentionCount);
        Assert.Single(response.Items);
        Assert.Equal("deleted", response.Items[0].Result);
        Assert.Equal("completed", response.Items[0].AuditStatus);
        Assert.Null(response.Items[0].SuggestedAction);
        ratingRepository.VerifyAll();
        deletionAdapter.VerifyAll();
        auditRepository.VerifyAll();
        Assert.True(logger.Contains(LogLevel.Information, "physically deleted item"));
        Assert.True(logger.Contains(LogLevel.Information, "Removed 1 rating rows"));
    }

    [Fact]
    public async Task DeleteItemsAsync_ReturnsDeletedWithSuggestedAction_WhenRatingCleanupFails()
    {
        Guid operatorUserId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        JellyfinDeletionTarget target = CreateTarget(itemId, "Cleanup Failure Item");
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IRatingRepository> ratingRepository = new(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteForItemsAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == itemId),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cleanup failed"));
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "requested"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditRepository
            .Setup(repository => repository.AddAsync(
                It.Is<DeleteAuditLog>(log => log.ItemId == itemId && log.Result == "deleted"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IJellyfinDeletionAdapter> deletionAdapter = new(MockBehavior.Strict);
        deletionAdapter
            .Setup(adapter => adapter.GetTargetAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        deletionAdapter
            .Setup(adapter => adapter.DeleteAsync(target, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        TestFeatureService featureService = new();
        TestLogger<DeletionService> logger = new();
        DeletionService service = CreateService(userManager, ratingRepository, auditRepository, featureService, deletionAdapter, logger);

        Models.Responses.PhysicalDeleteResponse response = await service.DeleteItemsAsync(operatorUserId, [itemId], true, CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal("deleted", response.Items[0].Result);
        Assert.Equal("completed", response.Items[0].AuditStatus);
        Assert.Contains("rating cleanup did not complete", response.Items[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SQLite", response.Items[0].SuggestedAction, StringComparison.Ordinal);
        Assert.Equal(1, response.AttentionCount);
        Assert.True(logger.Contains(LogLevel.Error, "failed to clean related rating rows"));
    }

    private static DeletionService CreateService(
        Mock<IUserManager> userManager,
        Mock<IRatingRepository> ratingRepository,
        Mock<IDeleteAuditLogRepository> auditRepository,
        IPluginFeatureService featureService,
        Mock<IJellyfinDeletionAdapter> deletionAdapter,
        TestLogger<DeletionService> logger)
    {
        return new DeletionService(
            userManager.Object,
            ratingRepository.Object,
            auditRepository.Object,
            featureService,
            deletionAdapter.Object,
            logger);
    }

    private static Mock<IUserManager> CreateUserManager(Guid userId, bool isAdministrator)
    {
        User user = new("test-user", "test-auth", "test-reset")
        {
            Id = userId
        };
        user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);

        Mock<IUserManager> userManager = new(MockBehavior.Strict);
        userManager.Setup(manager => manager.GetUserById(userId)).Returns(user);
        return userManager;
    }

    private static JellyfinDeletionTarget CreateTarget(Guid itemId, string itemName)
    {
        return new JellyfinDeletionTarget
        {
            ItemId = itemId,
            ItemName = itemName
        };
    }
}
