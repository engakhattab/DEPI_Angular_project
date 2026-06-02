using System.Text.Json;
using HR.API.Controllers;
using HR.API.Middleware;
using HR.Application.Departments;
using HR.Application.DTOs.Auth;
using HR.Application.DTOs.Departments;
using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Application.Auth;
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
        });

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

        public Task<PagedList<EmployeeResponse>> GetEmployeesAsync(HR.Domain.Enums.EmployeeStatus? status, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeCreatedResponse>> CreateEmployeeAsync(EmployeeCreateRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct)
            => Task.FromResult(UpdateResult ?? throw new NotSupportedException());

        public Task<Result> DeleteEmployeeAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
