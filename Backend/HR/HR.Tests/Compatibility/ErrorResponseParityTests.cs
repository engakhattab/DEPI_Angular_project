using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.API.Middleware;
using HR.Application.Departments;
using HR.Application.DTOs.Auth;
using HR.Application.DTOs.Departments;
using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Application.Auth;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace HR.Tests.Compatibility;

public class ErrorResponseParityTests
{
    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedPayload()
    {
        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Validation("Invalid credentials.", "VALIDATION"))));

        var result = await controller.Login(
            new LoginRequest { Identifier = "user@example.com", Password = "WrongPass1!" },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("VALIDATION", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid credentials.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task CreateDepartment_WhenConflictOccurs_ReturnsConflictPayload()
    {
        var controller = new DepartmentsController(new StubDepartmentService
        {
            CreateResult = Result<DepartmentResponse>.Failure(
                ServiceError.Conflict("Department exists.", "CONFLICT"))
        });

        var result = await controller.CreateDepartment(
            new DepartmentCreateRequest { Name = "Engineering" },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(conflict.Value));
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal("CONFLICT", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Department exists.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UpdateEmployee_WhenValidationFails_ReturnsBadRequestPayload()
    {
        var controller = new EmployeesController(new StubEmployeeService
        {
            UpdateResult = Result<EmployeeResponse>.Failure(
                ServiceError.Validation("Validation failed.", "VALIDATION"))
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", Guid.NewGuid().ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.UpdateEmployee(
            Guid.NewGuid(),
            new EmployeeUpdateRequest
            {
                FullName = "User",
                Email = "user@example.com",
                DepartmentId = Guid.NewGuid()
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(badRequest.Value));
        Assert.Equal("VALIDATION", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Validation failed.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UpdateEmployee_WhenBusinessRuleFails_ReturnsUnprocessableEntityPayload()
    {
        var controller = new EmployeesController(new StubEmployeeService
        {
            UpdateResult = Result<EmployeeResponse>.Failure(
                ServiceError.BusinessRule("Business rule failed."))
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", Guid.NewGuid().ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.UpdateEmployee(
            Guid.NewGuid(),
            new EmployeeUpdateRequest
            {
                FullName = "User",
                Email = "user@example.com",
                DepartmentId = Guid.NewGuid()
            },
            CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unprocessable.Value));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
        Assert.Equal("BUSINESS_RULE_VIOLATION", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Business rule failed.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_WhenAccountIsRevoked_ReturnsUnauthorizedPayload()
    {
        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Unauthorized("This employee account is no longer allowed to sign in."))));

        var result = await controller.Login(
            new LoginRequest { Identifier = "employee@example.com", Password = "ValidPass1!" },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("This employee account is no longer allowed to sign in.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetDepartment_WhenMissing_ReturnsNotFoundPayload()
    {
        var controller = new DepartmentsController(new StubDepartmentService());

        var result = await controller.GetDepartment(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(notFound.Value));
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Middleware_WhenUnexpectedExceptionOccurs_ReturnsGenericServerErrorPayload()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("secret details"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("SERVER_ERROR", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("An unexpected error occurred.", payload.RootElement.GetProperty("message").GetString());
    }

    private sealed class StubAuthService(Result<AuthenticatedEmployee> result) : IAuthService
    {
        public Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(string identifier, string password, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class StubDepartmentService : IDepartmentService
    {
        public Result<DepartmentResponse>? CreateResult { get; init; }

        public Task<PagedList<DepartmentResponse>> GetDepartmentsAsync(int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<DepartmentResponse?> GetDepartmentByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult<DepartmentResponse?>(null);

        public Task<Result<DepartmentResponse>> CreateDepartmentAsync(DepartmentCreateRequest request, CancellationToken ct)
            => Task.FromResult(CreateResult ?? throw new NotSupportedException());

        public Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid id, DepartmentUpdateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> DeleteDepartmentAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class StubEmployeeService : IEmployeeService
    {
        public Result<EmployeeResponse>? UpdateResult { get; init; }

        public Task<Result<PagedList<EmployeeResponse>>> GetEmployeesAsync(Guid requesterEmployeeId, EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(Guid requesterEmployeeId, EmployeeCreateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid requesterEmployeeId, Guid id, EmployeeUpdateRequest request, CancellationToken ct)
            => Task.FromResult(UpdateResult ?? throw new NotSupportedException());

        public Task<Result<EmployeeRoleResponse>> UpdateRoleAsync(Guid requesterEmployeeId, Guid id, EmployeeRoleUpdateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> DeleteEmployeeAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
