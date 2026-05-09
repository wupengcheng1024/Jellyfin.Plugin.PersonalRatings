using Jellyfin.Plugin.PersonalRatings.Controllers;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Jellyfin.Plugin.PersonalRatings.Models.Responses;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Controllers;

public sealed class AuditLogsControllerTests
{
    [Fact]
    public async Task QueryAuditLogs_ReturnsForbid_WhenServiceRejectsCaller()
    {
        Guid userId = Guid.NewGuid();
        Mock<IAuditLogService> auditLogService = new(MockBehavior.Strict);
        auditLogService
            .Setup(service => service.QueryAsync(
                userId,
                It.IsAny<AuditLogQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Delete audit logs require administrator privileges."));

        AuditLogsController controller = new(auditLogService.Object, NullLogger<AuditLogsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestClaimsPrincipalFactory.CreateAuthenticatedUser(userId)
            }
        };

        ActionResult<AuditLogQueryResponse> actionResult = await controller.QueryAuditLogs(new AuditLogQueryRequest(), CancellationToken.None);

        Assert.IsType<ForbidResult>(actionResult.Result);
    }

    [Fact]
    public async Task QueryAuditLogs_ReturnsOk_WhenServiceReturnsPage()
    {
        Guid userId = Guid.NewGuid();
        AuditLogQueryResponse response = new()
        {
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 25,
            Items =
            [
                new AuditLogListItemResponse
                {
                    Id = 1,
                    OperatorUserId = userId.ToString("D"),
                    ItemId = Guid.NewGuid().ToString("D"),
                    Result = "deleted",
                    Action = "deletePhysical",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        Mock<IAuditLogService> auditLogService = new(MockBehavior.Strict);
        auditLogService
            .Setup(service => service.QueryAsync(
                userId,
                It.IsAny<AuditLogQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        AuditLogsController controller = new(auditLogService.Object, NullLogger<AuditLogsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestClaimsPrincipalFactory.CreateAuthenticatedUser(userId)
            }
        };

        ActionResult<AuditLogQueryResponse> actionResult = await controller.QueryAuditLogs(new AuditLogQueryRequest(), CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        AuditLogQueryResponse payload = Assert.IsType<AuditLogQueryResponse>(okResult.Value);
        Assert.Equal(1, payload.TotalCount);
        Assert.Single(payload.Items);
    }
}
