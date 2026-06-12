using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.Audit;
using HR.Application.DTOs.Audit;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Audit;

public class AuditLogsControllerTests
{
    [Fact]
    public async Task Search_ReadsRequesterFromEmployeeClaimAndReturnsPagedAuditLogs()
    {
        var requesterId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var service = new RecordingAuditLogService
        {
            Result = Result<PagedList<AuditLogEntryResponse>>.Success(new PagedList<AuditLogEntryResponse>(
            [
                new AuditLogEntryResponse
                {
                    Id = Guid.NewGuid(),
                    EntityType = "Employee",
                    EntityId = entityId,
                    ActionType = AuditActionType.Updated,
                    ActorEmployeeId = actorId,
                    PerformedAt = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero),
                    ChangedFields = ["FullName"]
                }
            ], 1, 2, 10))
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.Search(
            entityType: "Employee",
            entityId: entityId,
            actorEmployeeId: actorId,
            action: "Updated",
            from: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2026, 6, 30, 23, 59, 59, TimeSpan.Zero),
            page: 2,
            pageSize: 10,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedList<AuditLogEntryResponse>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal(requesterId, service.RequesterEmployeeId);
        Assert.Equal("Employee", service.Request!.EntityType);
        Assert.Equal(entityId, service.Request.EntityId);
        Assert.Equal(actorId, service.Request.ActorEmployeeId);
        Assert.Equal("Updated", service.Request.Action);
        Assert.Equal(2, service.Request.Page);
        Assert.Equal(10, service.Request.PageSize);
    }

    [Fact]
    public async Task Search_WhenEmployeeClaimIsMissing_ReturnsUnauthorizedStructuredPayload()
    {
        var controller = new AuditLogsController(new RecordingAuditLogService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Search(ct: CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Search_WhenServiceReturnsForbidden_ReturnsStructuredForbiddenPayload()
    {
        var service = new RecordingAuditLogService
        {
            Result = Result<PagedList<AuditLogEntryResponse>>.Failure(ServiceError.Forbidden())
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Search(ct: CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(forbidden.Value));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Forbidden", payload.RootElement.GetProperty("message").GetString());
    }

    private static AuditLogsController CreateController(IAuditLogService service, Guid employeeId)
    {
        return new AuditLogsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", employeeId.ToString())
                    ], "test"))
                }
            }
        };
    }

    private sealed class RecordingAuditLogService : IAuditLogService
    {
        public Guid RequesterEmployeeId { get; private set; }

        public AuditLogQueryRequest? Request { get; private set; }

        public Result<PagedList<AuditLogEntryResponse>>? Result { get; init; }

        public Task<Result<PagedList<AuditLogEntryResponse>>> SearchAsync(
            Guid requesterEmployeeId,
            AuditLogQueryRequest request,
            CancellationToken ct)
        {
            RequesterEmployeeId = requesterEmployeeId;
            Request = request;

            return Task.FromResult(Result ?? throw new NotSupportedException());
        }
    }
}
