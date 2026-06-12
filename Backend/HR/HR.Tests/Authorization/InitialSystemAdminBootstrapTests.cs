using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Authorization;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.Data;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HR.Tests.Authorization;

public class InitialSystemAdminBootstrapTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoSystemAdminExistsAndConfigIsValid_CreatesAdminUserEmployeeRoleAndAudit()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var password = "BootPass1!";
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(environment.DefaultDepartment!.Id, password: password));

        await bootstrapper.ExecuteAsync(CancellationToken.None);

        var user = await environment.UserManager.FindByEmailAsync("bootstrap-admin@example.com");
        var employee = await environment.Context.Employees.SingleAsync(e => e.EmployeeNumber == "BOOT-001");
        var audit = await environment.Context.AuditLogEntries.SingleAsync();

        Assert.NotNull(user);
        Assert.Equal(user!.Id, employee.ApplicationUserId);
        Assert.Equal(EmployeeRole.SystemAdministrator, employee.Role);
        Assert.Equal(AuditActionType.InitialAdminCreated, audit.ActionType);
        Assert.Equal(InitialSystemAdminBootstrapper.SystemActorMarker, audit.ActorMarker);
        Assert.Contains("BOOT-001", audit.NewValues);
        Assert.Contains("bootstrap-admin@example.com", audit.NewValues);
        Assert.DoesNotContain(password, audit.NewValues ?? string.Empty);
        Assert.DoesNotContain(password, audit.OldValues ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRunTwice_DoesNotCreateDuplicates()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(environment.DefaultDepartment!.Id));

        await bootstrapper.ExecuteAsync(CancellationToken.None);
        await bootstrapper.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, await environment.Context.Users.CountAsync());
        Assert.Equal(1, await environment.Context.Employees.CountAsync());
        Assert.Equal(1, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSystemAdminAlreadyExists_DoesNothing()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var existing = await environment.AddEmployeeAsync(
            "SYS-001",
            "existing-admin@example.com",
            environment.DefaultDepartment!.Id,
            role: EmployeeRole.SystemAdministrator);
        var bootstrapper = CreateBootstrapper(environment, new InitialAdminBootstrapOptions());

        await bootstrapper.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, await environment.Context.Users.CountAsync());
        Assert.Equal(1, await environment.Context.Employees.CountAsync());
        Assert.Equal(EmployeeRole.SystemAdministrator, (await environment.Context.Employees.SingleAsync(e => e.Id == existing.Id)).Role);
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequiredConfigIsMissing_FailsWithoutPartialRecords()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var bootstrapper = CreateBootstrapper(environment, new InitialAdminBootstrapOptions { Enabled = true, Mode = InitialAdminBootstrapOptions.CreateInitialAdminMode });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Contains("EmployeeNumber", ex.Message);
        Assert.Equal(0, await environment.Context.Users.CountAsync());
        Assert.Equal(0, await environment.Context.Employees.CountAsync());
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsInvalid_FailsWithoutPartialRecords()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(environment.DefaultDepartment!.Id, password: "weak"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Contains("identity creation failed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await environment.Context.Users.CountAsync());
        Assert.Equal(0, await environment.Context.Employees.CountAsync());
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmployeeNumberAlreadyExists_FailsWithoutFallback()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var existing = await environment.AddEmployeeAsync("BOOT-001", "existing@example.com", environment.DefaultDepartment!.Id);
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(environment.DefaultDepartment.Id));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Contains("employee number already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EmployeeRole.Employee, (await environment.Context.Employees.SingleAsync(e => e.Id == existing.Id)).Role);
        Assert.False(await environment.Context.Employees.AnyAsync(e => e.Role == EmployeeRole.SystemAdministrator));
        Assert.Equal(1, await environment.Context.Users.CountAsync());
        Assert.Equal(1, await environment.Context.Employees.CountAsync());
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_FailsWithoutPartialRecords()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        await environment.AddEmployeeAsync("EMP-EXIST", "bootstrap-admin@example.com", environment.DefaultDepartment!.Id);
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(environment.DefaultDepartment.Id));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Contains("email already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await environment.Context.Employees.AnyAsync(e => e.Role == EmployeeRole.SystemAdministrator));
        Assert.Equal(1, await environment.Context.Users.CountAsync());
        Assert.Equal(1, await environment.Context.Employees.CountAsync());
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDepartmentIsMissing_FailsWithoutPartialRecords()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var bootstrapper = CreateBootstrapper(environment, ValidOptions(Guid.NewGuid()));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Contains("DepartmentId", ex.Message);
        Assert.Equal(0, await environment.Context.Users.CountAsync());
        Assert.Equal(0, await environment.Context.Employees.CountAsync());
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigIsInvalid_DoesNotFallbackToFirstActiveEmployee()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var existing = await environment.AddEmployeeAsync("EMP-ACTIVE", "active@example.com", environment.DefaultDepartment!.Id);
        var bootstrapper = CreateBootstrapper(environment, new InitialAdminBootstrapOptions { Enabled = false });

        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.ExecuteAsync(CancellationToken.None));

        Assert.Equal(EmployeeRole.Employee, (await environment.Context.Employees.SingleAsync(e => e.Id == existing.Id)).Role);
        Assert.False(await environment.Context.Employees.AnyAsync(e => e.Role == EmployeeRole.SystemAdministrator));
        Assert.Equal(0, await environment.Context.AuditLogEntries.CountAsync());
    }

    private static InitialSystemAdminBootstrapper CreateBootstrapper(
        SqliteTestEnvironment environment,
        InitialAdminBootstrapOptions options)
    {
        return new InitialSystemAdminBootstrapper(
            environment.GetRequiredService<IEmployeeRepository>(),
            environment.GetRequiredService<IDepartmentRepository>(),
            environment.GetRequiredService<IAuditWriter>(),
            environment.GetRequiredService<IUnitOfWork>(),
            environment.GetRequiredService<UserManager<ApplicationUser>>(),
            Options.Create(options),
            environment.GetRequiredService<TimeProvider>());
    }

    private static InitialAdminBootstrapOptions ValidOptions(Guid departmentId, string password = "BootPass1!")
    {
        return new InitialAdminBootstrapOptions
        {
            Enabled = true,
            Mode = InitialAdminBootstrapOptions.CreateInitialAdminMode,
            EmployeeNumber = "BOOT-001",
            Email = "bootstrap-admin@example.com",
            FullName = "Bootstrap Admin",
            DepartmentId = departmentId,
            TemporaryPassword = password,
            ForcePasswordChange = true
        };
    }
}
