using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Authorization;

public class EmployeeRoleControllerTests
{
    [Fact]
    public void UpdateRole_IsProtectedBySystemAdministratorPolicy()
    {
        var method = typeof(EmployeesController).GetMethod(nameof(EmployeesController.UpdateRole), BindingFlags.Instance | BindingFlags.Public);

        var authorize = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("SystemAdministrator", authorize.Policy);
    }

    [Fact]
    public async Task UpdateRole_ReadsRequesterIdFromEmployeeClaimAndReturnsRoleResponse()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var service = new RecordingEmployeeService
        {
            UpdateRoleResult = Result<EmployeeRoleResponse>.Success(new EmployeeRoleResponse
            {
                EmployeeId = employeeId,
                Role = EmployeeRole.Manager,
                UpdatedAt = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)
            })
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.UpdateRole(
            employeeId,
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.Manager },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<EmployeeRoleResponse>(ok.Value);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.Equal(EmployeeRole.Manager, payload.Role);
        Assert.Equal(requesterId, service.RequesterEmployeeId);
        Assert.Equal(employeeId, service.TargetEmployeeId);
        Assert.Equal(EmployeeRole.Manager, service.Request!.Role);
    }

    [Fact]
    public async Task UpdateRole_WhenEmployeeClaimIsMissing_ReturnsUnauthorizedStructuredPayload()
    {
        var controller = new EmployeesController(new RecordingEmployeeService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.UpdateRole(
            Guid.NewGuid(),
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.Manager },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UpdateRole_WhenServiceReturnsForbidden_ReturnsStructuredForbiddenPayload()
    {
        var service = new RecordingEmployeeService
        {
            UpdateRoleResult = Result<EmployeeRoleResponse>.Failure(ServiceError.Forbidden())
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.UpdateRole(
            Guid.NewGuid(),
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.HRAdministrator },
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(forbidden.Value));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Forbidden", payload.RootElement.GetProperty("message").GetString());
    }

    private static EmployeesController CreateController(IEmployeeService service, Guid employeeId)
    {
        return new EmployeesController(service)
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

    private sealed class RecordingEmployeeService : IEmployeeService
    {
        public Guid RequesterEmployeeId { get; private set; }
        public Guid TargetEmployeeId { get; private set; }
        public EmployeeRoleUpdateRequest? Request { get; private set; }
        public Result<EmployeeRoleResponse>? UpdateRoleResult { get; init; }

        public Task<PagedList<EmployeeResponse>> GetEmployeesAsync(EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(EmployeeCreateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeRoleResponse>> UpdateRoleAsync(Guid requesterEmployeeId, Guid id, EmployeeRoleUpdateRequest request, CancellationToken ct)
        {
            RequesterEmployeeId = requesterEmployeeId;
            TargetEmployeeId = id;
            Request = request;
            return Task.FromResult(UpdateRoleResult ?? throw new NotSupportedException());
        }

        public Task<Result> DeleteEmployeeAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
