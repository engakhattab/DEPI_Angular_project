using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.API.Middleware;
using HR.Application.Auth;
using HR.Application.DTOs.Auth;
using HR.Application.DTOs.Employees;
using HR.Domain.Enums;
using HR.Domain.Exceptions;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HR.Tests.Authorization;

public class Phase8AuthCompatibilityBoundaryTests
{
    [Fact]
    public async Task Login_ReturnsEmployeeWrapperAndSignsInWithExistingClaims()
    {
        var authenticationService = new RecordingAuthenticationService();
        var employee = CreateEmployee(EmployeeRole.HRAdministrator);
        var controller = CreateController(
            Result<AuthenticatedEmployee>.Success(
                new AuthenticatedEmployee(employee, employee.IdentityUserId, employee.UserName, employee.Email)),
            authenticationService);

        var result = await controller.Login(
            new LoginRequest { Identifier = employee.Email, Password = "ValidPass1!" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponse>(ok.Value);
        Assert.NotNull(payload.Employee);
        Assert.Equal(employee.Id, payload.Employee.Id);
        Assert.Equal(employee.EmployeeNumber, payload.Employee.EmployeeNumber);
        Assert.Equal(employee.FullName, payload.Employee.FullName);
        Assert.Equal(employee.Email, payload.Employee.Email);
        Assert.Equal(employee.DepartmentId, payload.Employee.DepartmentId);
        Assert.Equal(employee.DepartmentName, payload.Employee.DepartmentName);
        Assert.Equal(employee.Status, payload.Employee.Status);
        Assert.Equal(employee.Role, payload.Employee.Role);
        Assert.Equal(employee.VacationBalanceDays, payload.Employee.VacationBalanceDays);
        Assert.Equal(employee.IdentityUserId, payload.Employee.IdentityUserId);
        Assert.Equal(employee.UserName, payload.Employee.UserName);

        var principal = Assert.IsType<ClaimsPrincipal>(authenticationService.SignedInPrincipal);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authenticationService.Scheme);
        Assert.False(authenticationService.Properties!.IsPersistent);
        Assert.Equal(employee.IdentityUserId, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(employee.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(employee.Id.ToString(), principal.FindFirstValue("employee_id"));
        Assert.Equal(employee.EmployeeNumber, principal.FindFirstValue("employee_number"));
        Assert.Equal(employee.FullName, principal.FindFirstValue("full_name"));
        Assert.Equal(employee.Role.ToString(), principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal(employee.Role.ToString(), principal.FindFirstValue("employee_role"));
    }

    [Fact]
    public async Task Login_WhenAuthFails_ReturnsExistingUnauthorizedShape()
    {
        var controller = CreateController(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Unauthorized("Invalid credentials.")));

        var result = await controller.Login(
            new LoginRequest { Identifier = "missing@example.com", Password = "wrong" },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        AssertStructuredPayload(unauthorized.Value, "UNAUTHORIZED", "Invalid credentials.");
    }

    [Fact]
    public void LoginDtos_KeepRequestAndResponseCompatibility()
    {
        var requestProperties = PublicPropertyNames<LoginRequest>();
        var responseProperties = PublicPropertyNames<LoginResponse>();

        Assert.Contains("Identifier", requestProperties);
        Assert.Contains("Password", requestProperties);
        Assert.Contains("Employee", responseProperties);
    }

    [Fact]
    public void Me_UsesEmployeeIdAndRoleClaimsForCurrentUserResponse()
    {
        var employeeId = Guid.NewGuid();
        var controller = CreateController(
            Result<AuthenticatedEmployee>.Failure(ServiceError.Unauthorized("Invalid credentials.")),
            user: new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("employee_id", employeeId.ToString()),
                new Claim("full_name", "Current User"),
                new Claim(ClaimTypes.Email, "current@example.com"),
                new Claim("employee_role", EmployeeRole.Manager.ToString())
            ], CookieAuthenticationDefaults.AuthenticationScheme)));

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CurrentUserResponse>(ok.Value);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.Equal("Current User", payload.FullName);
        Assert.Equal("current@example.com", payload.Email);
        Assert.Equal(EmployeeRole.Manager, payload.Role);
    }

    [Fact]
    public void Me_WhenEmployeeIdClaimIsMissing_ReturnsExistingUnauthorizedShape()
    {
        var controller = CreateController(
            Result<AuthenticatedEmployee>.Failure(ServiceError.Unauthorized("Invalid credentials.")),
            user: new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("full_name", "Current User"),
                new Claim(ClaimTypes.Email, "current@example.com")
            ], CookieAuthenticationDefaults.AuthenticationScheme)));

        var result = controller.Me();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        AssertStructuredPayload(unauthorized.Value, "UNAUTHORIZED", "Invalid session.");
    }

    [Theory]
    [InlineData("not-found", 404, "NOT_FOUND", "Missing employee.")]
    [InlineData("conflict", 409, "CONFLICT", "Conflicting state.")]
    [InlineData("business", 422, "BUSINESS_RULE", "Rule failed.")]
    [InlineData("unexpected", 500, "SERVER_ERROR", "An unexpected error occurred.")]
    public async Task GlobalExceptionMiddleware_WritesStructuredStatusPayloads(
        string scenario,
        int expectedStatus,
        string expectedCode,
        string expectedMessage)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw scenario switch
            {
                "not-found" => new NotFoundException(expectedMessage),
                "conflict" => new ConflictException(expectedMessage),
                "business" => new BusinessRuleException(expectedMessage),
                _ => new InvalidOperationException("Unexpected")
            },
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);
        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedCode, payload.RootElement.GetProperty("code").GetString());
        Assert.Equal(expectedMessage, payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void Program_KeepsCookieAuthenticationCompatibilityConventions()
    {
        var program = ReadRepositoryFile("HR.API", "Program.cs");

        Assert.Contains("AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)", program);
        Assert.Contains("CookieAuthenticationDefaults.AuthenticationScheme", program);
        Assert.Contains("options.Cookie.HttpOnly = true", program);
        Assert.Contains("options.Cookie.SameSite = SameSiteMode.Strict", program);
        Assert.Contains("options.Cookie.SecurePolicy = CookieSecurePolicy.Always", program);
        Assert.Contains("context.Response.StatusCode = StatusCodes.Status401Unauthorized", program);
        Assert.Contains("code = \"UNAUTHORIZED\"", program);
        Assert.Contains("context.Response.StatusCode = StatusCodes.Status403Forbidden", program);
        Assert.Contains("code = \"FORBIDDEN\"", program);
        Assert.Contains("context.RejectPrincipal();", program);
        Assert.Contains("SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)", program);
    }

    private static AuthController CreateController(
        Result<AuthenticatedEmployee> authResult,
        RecordingAuthenticationService? authenticationService = null,
        ClaimsPrincipal? user = null)
    {
        var services = new ServiceCollection();
        if (authenticationService is not null)
        {
            services.AddSingleton<IAuthenticationService>(authenticationService);
        }

        return new AuthController(new StubAuthService(authResult))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services.BuildServiceProvider(),
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private static EmployeeResponse CreateEmployee(EmployeeRole role)
    {
        return new EmployeeResponse
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "PH8-AUTH-001",
            FullName = "Phase 8 Auth User",
            Email = "phase8-auth@example.com",
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            Status = EmployeeStatus.Active,
            Role = role,
            VacationBalanceDays = 21,
            IsDeleted = false,
            IdentityUserId = "phase8-auth-user",
            UserName = "phase8-auth@example.com"
        };
    }

    private static HashSet<string> PublicPropertyNames<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet();
    }

    private static void AssertStructuredPayload(object? value, string expectedCode, string expectedMessage)
    {
        var payload = JsonSerializer.SerializeToElement(value);
        Assert.Equal(expectedCode, payload.GetProperty("code").GetString());
        Assert.Equal(expectedMessage, payload.GetProperty("message").GetString());
    }

    private static string ReadRepositoryFile(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HR.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(new[] { directory!.FullName }.Concat(relativePath).ToArray()));
    }

    private sealed class StubAuthService(Result<AuthenticatedEmployee> result) : IAuthService
    {
        public Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(string identifier, string password, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public string? Scheme { get; private set; }
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }
        public AuthenticationProperties? Properties { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            Scheme = scheme;
            SignedInPrincipal = principal;
            Properties = properties;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
