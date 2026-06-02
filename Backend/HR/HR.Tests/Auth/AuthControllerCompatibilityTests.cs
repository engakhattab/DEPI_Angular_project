using System.Security.Claims;
using HR.API.Controllers;
using HR.Application.Auth;
using HR.Application.DTOs.Auth;
using HR.Application.DTOs.Employees;
using HR.Domain.Enums;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HR.Tests.Auth;

public class AuthControllerCompatibilityTests
{
    [Fact]
    public async Task Login_ReturnsExistingEmployeePayloadAndExpectedClaims()
    {
        var authenticationService = new RecordingAuthenticationService();
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authenticationService)
            .BuildServiceProvider();

        var employee = new EmployeeResponse
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP-501",
            FullName = "Compatibility Employee",
            Email = "compat@example.com",
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            Status = EmployeeStatus.Active,
            IdentityUserId = "user-501",
            UserName = "compat@example.com"
        };

        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Success(
            new AuthenticatedEmployee(employee, "user-501", employee.UserName, employee.Email))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };

        var result = await controller.Login(
            new LoginRequest { Identifier = "compat@example.com", Password = "ValidPass1!" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal(employee.EmployeeNumber, payload.Employee.EmployeeNumber);
        Assert.Equal(employee.FullName, payload.Employee.FullName);
        Assert.NotNull(authenticationService.SignedInPrincipal);
        Assert.Equal("user-501", authenticationService.SignedInPrincipal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(employee.Email, authenticationService.SignedInPrincipal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(employee.Id.ToString(), authenticationService.SignedInPrincipal.FindFirstValue("employee_id"));
        Assert.Equal(employee.EmployeeNumber, authenticationService.SignedInPrincipal.FindFirstValue("employee_number"));
        Assert.Equal(employee.FullName, authenticationService.SignedInPrincipal.FindFirstValue("full_name"));
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authenticationService.Scheme);
        Assert.False(authenticationService.Properties!.IsPersistent);
    }

    [Fact]
    public async Task Logout_SignsOutAndReturnsNoContent()
    {
        var authenticationService = new RecordingAuthenticationService();
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authenticationService)
            .BuildServiceProvider();

        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Validation("Invalid credentials.", "VALIDATION"))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authenticationService.SignOutScheme);
    }

    [Fact]
    public void Me_ReturnsCurrentUserResponseFromClaims()
    {
        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Validation("Invalid credentials.", "VALIDATION"))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", Guid.Parse("11111111-1111-1111-1111-111111111111").ToString()),
                        new Claim("full_name", "Current User"),
                        new Claim(ClaimTypes.Email, "current@example.com")
                    ], "test"))
                }
            }
        };

        var result = controller.Me();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CurrentUserResponse>(ok.Value);

        Assert.Equal("Current User", payload.FullName);
        Assert.Equal("current@example.com", payload.Email);
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
        public string? SignOutScheme { get; private set; }

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
        {
            SignOutScheme = scheme;
            return Task.CompletedTask;
        }
    }
}
