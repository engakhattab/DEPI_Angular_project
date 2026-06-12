using System.Security.Claims;
using System.Text.Json;
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

public class AuthRoleCompatibilityTests
{
    [Fact]
    public async Task Login_KeepsExistingEmployeeWrapperFieldsAndAddsRoleWithoutRemovingData()
    {
        var authenticationService = new RecordingAuthenticationService();
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authenticationService)
            .BuildServiceProvider();
        var employee = new EmployeeResponse
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "AUTH-RBAC-001",
            FullName = "Role User",
            Email = "role-user@example.com",
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            ManagerId = Guid.NewGuid(),
            ManagerName = "Manager User",
            BirthDate = new DateOnly(1995, 1, 1),
            JoinDate = new DateOnly(2024, 1, 1),
            JobTitle = "Analyst",
            PhoneNumber = "01000000000",
            Notes = "Existing notes",
            Status = EmployeeStatus.Active,
            Role = EmployeeRole.HRAdministrator,
            VacationBalanceDays = 21,
            IsDeleted = false,
            IdentityUserId = "auth-rbac-user",
            UserName = "role-user@example.com"
        };
        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Success(
            new AuthenticatedEmployee(employee, employee.IdentityUserId, employee.UserName, employee.Email))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };

        var result = await controller.Login(
            new LoginRequest { Identifier = employee.Email, Password = "ValidPass1!" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var login = Assert.IsType<LoginResponse>(ok.Value);
        var payload = JsonSerializer.SerializeToElement(login);
        Assert.True(payload.TryGetProperty("Employee", out var employeeElement) || payload.TryGetProperty("employee", out employeeElement));
        Assert.Equal(employee.EmployeeNumber, login.Employee.EmployeeNumber);
        Assert.Equal(employee.FullName, login.Employee.FullName);
        Assert.Equal(employee.Email, login.Employee.Email);
        Assert.Equal(employee.DepartmentId, login.Employee.DepartmentId);
        Assert.Equal(employee.DepartmentName, login.Employee.DepartmentName);
        Assert.Equal(employee.ManagerId, login.Employee.ManagerId);
        Assert.Equal(employee.ManagerName, login.Employee.ManagerName);
        Assert.Equal(employee.Status, login.Employee.Status);
        Assert.Equal(employee.VacationBalanceDays, login.Employee.VacationBalanceDays);
        Assert.Equal(employee.IdentityUserId, login.Employee.IdentityUserId);
        Assert.Equal(employee.UserName, login.Employee.UserName);
        Assert.Equal(EmployeeRole.HRAdministrator, login.Employee.Role);
        Assert.True(employeeElement.TryGetProperty("Role", out _) || employeeElement.TryGetProperty("role", out _));
        Assert.NotNull(authenticationService.SignedInPrincipal);
        var principal = authenticationService.SignedInPrincipal;
        Assert.Equal(EmployeeRole.HRAdministrator.ToString(), principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal(EmployeeRole.HRAdministrator.ToString(), principal.FindFirstValue("employee_role"));
    }

    [Fact]
    public void Me_KeepsExistingFieldsAndAddsRoleFromClaims()
    {
        var employeeId = Guid.NewGuid();
        var controller = new AuthController(new StubAuthService(Result<AuthenticatedEmployee>.Failure(
            ServiceError.Validation("Invalid credentials.", "VALIDATION"))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", employeeId.ToString()),
                        new Claim("full_name", "Current Role User"),
                        new Claim(ClaimTypes.Email, "current-role@example.com"),
                        new Claim("employee_role", EmployeeRole.Manager.ToString())
                    ], "test"))
                }
            }
        };

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CurrentUserResponse>(ok.Value);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.Equal("Current Role User", payload.FullName);
        Assert.Equal("current-role@example.com", payload.Email);
        Assert.Equal(EmployeeRole.Manager, payload.Role);
    }

    private sealed class StubAuthService(Result<AuthenticatedEmployee> result) : IAuthService
    {
        public Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(string identifier, string password, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, scheme);
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
