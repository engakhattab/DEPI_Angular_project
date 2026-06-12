using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.Dashboard;
using HR.Application.DTOs.Dashboard;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Dashboard;

public class DashboardControllerTests
{
    [Fact]
    public async Task Summary_ReadsRequesterClaimAndReturnsDashboardSummary()
    {
        var requesterId = Guid.NewGuid();
        var service = new RecordingDashboardService
        {
            Result = Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse
            {
                TotalActiveEmployees = 2,
                TotalDepartments = null,
                PendingVacationRequests = 1,
                EmployeesPerDepartment = new Dictionary<string, int> { ["Engineering"] = 2 },
                VacationRequestsByStatus = new Dictionary<string, int> { ["Pending"] = 1 }
            })
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.Summary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DashboardSummaryResponse>(ok.Value);
        Assert.Equal(2, payload.TotalActiveEmployees);
        Assert.Null(payload.TotalDepartments);
        Assert.Equal(requesterId, service.RequesterEmployeeId);
    }

    [Fact]
    public async Task Summary_WhenEmployeeClaimIsMissing_ReturnsUnauthorizedStructuredPayload()
    {
        var controller = new DashboardController(new RecordingDashboardService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Summary(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Summary_WhenServiceReturnsForbidden_ReturnsStructuredForbiddenPayload()
    {
        var service = new RecordingDashboardService
        {
            Result = Result<DashboardSummaryResponse>.Failure(ServiceError.Forbidden())
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Summary(CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(forbidden.Value));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Summary_ManagerPayloadCanHideDepartmentTotalWithoutExposingOrganizationWideValue()
    {
        var service = new RecordingDashboardService
        {
            Result = Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse
            {
                TotalActiveEmployees = 2,
                TotalDepartments = null,
                PendingVacationRequests = 1
            })
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Summary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DashboardSummaryResponse>(ok.Value);
        Assert.Null(payload.TotalDepartments);
        Assert.Equal(2, payload.TotalActiveEmployees);
    }

    private static DashboardController CreateController(IDashboardService service, Guid requesterId)
    {
        return new DashboardController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", requesterId.ToString())
                    ], "test"))
                }
            }
        };
    }

    private sealed class RecordingDashboardService : IDashboardService
    {
        public Guid RequesterEmployeeId { get; private set; }
        public Result<DashboardSummaryResponse>? Result { get; init; }

        public Task<Result<DashboardSummaryResponse>> GetSummaryAsync(Guid requesterEmployeeId, CancellationToken ct)
        {
            RequesterEmployeeId = requesterEmployeeId;
            return Task.FromResult(Result ?? throw new NotSupportedException());
        }
    }
}
