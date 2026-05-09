using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PersonalRatings.Data;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Services;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task QueryAsync_ThrowsUnauthorizedAccessException_WhenOperatorIsNotAdministrator()
    {
        Guid operatorUserId = Guid.NewGuid();
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: false);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        TestLogger<AuditLogService> logger = new();
        AuditLogService service = new(userManager.Object, auditRepository.Object, logger);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.QueryAsync(operatorUserId, new AuditLogQueryRequest(), CancellationToken.None));

        Assert.True(logger.Contains(LogLevel.Warning, "Rejected delete audit-log query from non-administrator user"));
    }

    [Fact]
    public async Task QueryAsync_ReturnsPagedAuditLogs_WhenOperatorIsAdministrator()
    {
        Guid operatorUserId = Guid.NewGuid();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        Mock<IUserManager> userManager = CreateUserManager(operatorUserId, isAdministrator: true);
        Mock<IDeleteAuditLogRepository> auditRepository = new(MockBehavior.Strict);
        auditRepository
            .Setup(repository => repository.QueryPageAsync(
                It.IsAny<AuditLogQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedQueryResult<DeleteAuditLog>
            {
                TotalCount = 1,
                Items =
                [
                    new DeleteAuditLog
                    {
                        Id = 7,
                        OperatorUserId = operatorUserId,
                        ItemId = Guid.NewGuid(),
                        ItemName = "Audit Item",
                        Action = "deletePhysical",
                        Result = "deleted",
                        Message = "The item was deleted from Jellyfin.",
                        CreatedAt = createdAt
                    }
                ]
            });

        TestLogger<AuditLogService> logger = new();
        AuditLogService service = new(userManager.Object, auditRepository.Object, logger);

        Models.Responses.AuditLogQueryResponse response = await service.QueryAsync(
            operatorUserId,
            new AuditLogQueryRequest
            {
                PageNumber = 2,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(1, response.TotalCount);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Single(response.Items);
        Assert.Equal("deleted", response.Items[0].Result);
        Assert.Equal(createdAt, response.Items[0].CreatedAt);
        auditRepository.VerifyAll();
    }

    private static Mock<IUserManager> CreateUserManager(Guid userId, bool isAdministrator)
    {
        User user = new("audit-test-user", "audit-auth", "audit-reset")
        {
            Id = userId
        };

        user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);

        Mock<IUserManager> userManager = new(MockBehavior.Strict);
        userManager
            .Setup(manager => manager.GetUserById(userId))
            .Returns(user);

        return userManager;
    }
}
