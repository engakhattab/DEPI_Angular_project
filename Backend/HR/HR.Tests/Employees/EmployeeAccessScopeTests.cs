using HR.Application.DTOs.Employees;
using HR.Application.Employees;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Employees;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using HR.Shared.Results;
using HR.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HR.Tests.Employees;

public class EmployeeAccessScopeTests
{
    [Fact]
    public async Task Employee_List_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.EmployeeId, null, 1, 25, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Employee_SelfDetail_ReturnsOk()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.EmployeeId, scope.Employee.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(scope.Employee.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Employee_OtherDetail_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.EmployeeId, scope.OtherEmployee.Id, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Employee_Detail_MissingTargetReturnsNotFound()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.EmployeeId, Guid.NewGuid(), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Manager_List_ContainsOnlyDirectAndIndirectActiveReports()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.ManagerId, null, 1, 100, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var employeeNumbers = result.Value!.Items.Select(e => e.EmployeeNumber).ToHashSet();
        Assert.Contains(scope.DirectReport.EmployeeNumber, employeeNumbers);
        Assert.Contains(scope.IndirectReport.EmployeeNumber, employeeNumbers);
        Assert.DoesNotContain(scope.Manager.EmployeeNumber, employeeNumbers);
        Assert.DoesNotContain(scope.OtherEmployee.EmployeeNumber, employeeNumbers);
    }

    [Fact]
    public async Task Manager_List_ExcludesSoftDeletedAndTerminatedReports()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.ManagerId, null, 1, 100, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var employeeNumbers = result.Value!.Items.Select(e => e.EmployeeNumber).ToHashSet();
        Assert.DoesNotContain(scope.TerminatedReport.EmployeeNumber, employeeNumbers);
        Assert.DoesNotContain(scope.DeletedReport.EmployeeNumber, employeeNumbers);
    }

    [Fact]
    public async Task Manager_NoTeam_ReturnsEmptyPage()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.NoTeamManagerId, null, 1, 25, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Manager_Detail_SelfAllowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.ManagerId, scope.Manager.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(scope.Manager.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Manager_Detail_TeamAllowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.ManagerId, scope.DirectReport.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(scope.DirectReport.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Manager_Detail_OutOfScopeReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.ManagerId, scope.OtherEmployee.Id, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Manager_Detail_MissingTargetReturnsNotFound()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeeByIdAsync(scope.ManagerId, Guid.NewGuid(), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task HRAdministrator_List_IncludesActiveSuspendedTerminatedAndSoftDeleted()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.HRAdminId, null, 1, 100, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var employeeNumbers = result.Value!.Items.Select(e => e.EmployeeNumber).ToHashSet();
        Assert.Contains(scope.Employee.EmployeeNumber, employeeNumbers);
        Assert.Contains(scope.TerminatedReport.EmployeeNumber, employeeNumbers);
        Assert.Contains(scope.DeletedReport.EmployeeNumber, employeeNumbers);
    }

    [Fact]
    public async Task SystemAdministrator_List_IncludesActiveSuspendedTerminatedAndSoftDeleted()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.SystemAdminId, null, 1, 100, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var employeeNumbers = result.Value!.Items.Select(e => e.EmployeeNumber).ToHashSet();
        Assert.Contains(scope.Employee.EmployeeNumber, employeeNumbers);
        Assert.Contains(scope.TerminatedReport.EmployeeNumber, employeeNumbers);
        Assert.Contains(scope.DeletedReport.EmployeeNumber, employeeNumbers);
    }

    [Fact]
    public async Task HRAdministrator_Detail_TerminatedAndSoftDeletedAllowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var terminated = await scope.Service.GetEmployeeByIdAsync(scope.HRAdminId, scope.TerminatedReport.Id, CancellationToken.None);
        Assert.True(terminated.IsSuccess);
        var deleted = await scope.Service.GetEmployeeByIdAsync(scope.HRAdminId, scope.DeletedReport.Id, CancellationToken.None);
        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task SystemAdministrator_Detail_TerminatedAndSoftDeletedAllowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var terminated = await scope.Service.GetEmployeeByIdAsync(scope.SystemAdminId, scope.TerminatedReport.Id, CancellationToken.None);
        Assert.True(terminated.IsSuccess);
        var deleted = await scope.Service.GetEmployeeByIdAsync(scope.SystemAdminId, scope.DeletedReport.Id, CancellationToken.None);
        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task OrganizationScope_StatusFilterAppliesWithinScope()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.GetEmployeesAsync(scope.HRAdminId, EmployeeStatus.Terminated, 1, 100, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.All(result.Value!.Items, e => Assert.Equal(EmployeeStatus.Terminated, e.Status));
    }

    [Fact]
    public async Task EmployeeAndManager_Create_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeCreateRequest
        {
            EmployeeNumber = "NEW-001",
            FullName = "New Employee",
            Email = "new@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active,
            InitialPassword = "ValidPass1!"
        };
        var employeeResult = await scope.Service.CreateEmployeeAsync(scope.EmployeeId, request, CancellationToken.None);
        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        var managerResult = await scope.Service.CreateEmployeeAsync(scope.ManagerId, request, CancellationToken.None);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task HRAdministrator_Create_AllowedForNonSystemAdministrator()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeCreateRequest
        {
            EmployeeNumber = "NEW-002",
            FullName = "New Employee",
            Email = "new2@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active,
            InitialPassword = "ValidPass1!"
        };
        var result = await scope.Service.CreateEmployeeAsync(scope.HRAdminId, request, CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HRAdministrator_UpdateSystemAdministrator_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.UpdateEmployeeAsync(
            scope.HRAdminId,
            scope.SystemAdmin.Id,
            new EmployeeUpdateRequest
            {
                FullName = "Hacked Name",
                Email = scope.SystemAdmin.Email ?? "admin@example.com",
                DepartmentId = scope.DefaultDepartmentId,
                Status = EmployeeStatus.Active
            },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task HRAdministrator_DeleteSystemAdministrator_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.DeleteEmployeeAsync(scope.HRAdminId, scope.SystemAdmin.Id, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task SystemAdministrator_DeleteLastActiveAdmin_ReturnsBusinessRuleError()
    {
        await using var scope = await AccessScopeFixture.CreateSingleAdminAsync();
        var result = await scope.Service.DeleteEmployeeAsync(scope.SystemAdminId, scope.SystemAdmin.Id, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task SystemAdministrator_UpdateLastActiveAdmin_ReturnsBusinessRuleError()
    {
        await using var scope = await AccessScopeFixture.CreateSingleAdminAsync();
        var result = await scope.Service.UpdateEmployeeAsync(
            scope.SystemAdminId,
            scope.SystemAdmin.Id,
            new EmployeeUpdateRequest
            {
                FullName = "Updated Name",
                Email = scope.SystemAdmin.Email ?? "admin@example.com",
                DepartmentId = scope.DefaultDepartmentId,
                Status = EmployeeStatus.Active
            },
            CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SystemAdministrator_UpdateNonLastActiveAdmin_Allowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.UpdateEmployeeAsync(
            scope.SystemAdminId,
            scope.SystemAdmin2.Id,
            new EmployeeUpdateRequest
            {
                FullName = "Updated Name",
                Email = scope.SystemAdmin2.Email ?? "admin2@example.com",
                DepartmentId = scope.DefaultDepartmentId,
                Status = EmployeeStatus.Active
            },
            CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SystemAdministrator_DeleteNonLastActiveAdmin_Allowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.DeleteEmployeeAsync(scope.SystemAdminId, scope.SystemAdmin2.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HRAdministrator_UpdateMissingTarget_ReturnsNotFound()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeUpdateRequest
        {
            FullName = "Missing",
            Email = "missing@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active
        };

        var result = await scope.Service.UpdateEmployeeAsync(scope.HRAdminId, Guid.NewGuid(), request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task SystemAdministrator_DeleteMissingTarget_ReturnsNotFound()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.DeleteEmployeeAsync(scope.SystemAdminId, Guid.NewGuid(), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_Update_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeUpdateRequest
        {
            FullName = "Updated",
            Email = scope.Employee.Email ?? "emp@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active
        };
        var employeeResult = await scope.Service.UpdateEmployeeAsync(scope.EmployeeId, scope.OtherEmployee.Id, request, CancellationToken.None);
        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        var managerResult = await scope.Service.UpdateEmployeeAsync(scope.ManagerId, scope.OtherEmployee.Id, request, CancellationToken.None);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_UpdateMissingTarget_ReturnsForbiddenBeforeTargetLookupResult()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeUpdateRequest
        {
            FullName = "Updated",
            Email = "probe@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active
        };
        var missingId = Guid.NewGuid();

        var employeeResult = await scope.Service.UpdateEmployeeAsync(scope.EmployeeId, missingId, request, CancellationToken.None);
        var managerResult = await scope.Service.UpdateEmployeeAsync(scope.ManagerId, missingId, request, CancellationToken.None);

        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_UpdateSoftDeletedTarget_ReturnsForbiddenBeforeTargetLookupResult()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var request = new EmployeeUpdateRequest
        {
            FullName = "Updated",
            Email = scope.DeletedReport.Email ?? "deleted@example.com",
            DepartmentId = scope.DefaultDepartmentId,
            Status = EmployeeStatus.Active
        };

        var employeeResult = await scope.Service.UpdateEmployeeAsync(scope.EmployeeId, scope.DeletedReport.Id, request, CancellationToken.None);
        var managerResult = await scope.Service.UpdateEmployeeAsync(scope.ManagerId, scope.DeletedReport.Id, request, CancellationToken.None);

        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_Delete_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var employeeResult = await scope.Service.DeleteEmployeeAsync(scope.EmployeeId, scope.OtherEmployee.Id, CancellationToken.None);
        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        var managerResult = await scope.Service.DeleteEmployeeAsync(scope.ManagerId, scope.OtherEmployee.Id, CancellationToken.None);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_DeleteMissingTarget_ReturnsForbiddenBeforeTargetLookupResult()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var missingId = Guid.NewGuid();

        var employeeResult = await scope.Service.DeleteEmployeeAsync(scope.EmployeeId, missingId, CancellationToken.None);
        var managerResult = await scope.Service.DeleteEmployeeAsync(scope.ManagerId, missingId, CancellationToken.None);

        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task EmployeeAndManager_DeleteSoftDeletedTarget_ReturnsForbiddenBeforeIdempotentSuccess()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();

        var employeeResult = await scope.Service.DeleteEmployeeAsync(scope.EmployeeId, scope.DeletedReport.Id, CancellationToken.None);
        var managerResult = await scope.Service.DeleteEmployeeAsync(scope.ManagerId, scope.DeletedReport.Id, CancellationToken.None);

        Assert.False(employeeResult.IsSuccess);
        Assert.Equal("FORBIDDEN", employeeResult.Error!.Code);
        Assert.False(managerResult.IsSuccess);
        Assert.Equal("FORBIDDEN", managerResult.Error!.Code);
    }

    [Fact]
    public async Task HRAdministrator_UpdateNonSystemAdministrator_Allowed()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.UpdateEmployeeAsync(
            scope.HRAdminId,
            scope.Employee.Id,
            new EmployeeUpdateRequest
            {
                FullName = "Updated By HR",
                Email = scope.Employee.Email ?? "emp@example.com",
                DepartmentId = scope.DefaultDepartmentId,
                Status = EmployeeStatus.Active
            },
            CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated By HR", result.Value!.FullName);
    }

    [Fact]
    public async Task HRAdministrator_UpdateRole_ReturnsForbidden()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();
        var result = await scope.Service.UpdateRoleAsync(
            scope.HRAdminId,
            scope.Employee.Id,
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.Manager },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task SystemAdministrator_DemoteLastActiveAdmin_ReturnsBusinessRuleAndDoesNotMutate()
    {
        await using var scope = await AccessScopeFixture.CreateSingleAdminAsync();

        var result = await scope.Service.UpdateRoleAsync(
            scope.SystemAdminId,
            scope.SystemAdmin.Id,
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.Employee },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);

        scope.Environment.Context.ChangeTracker.Clear();
        var reloaded = await scope.Environment.Context.Employees.SingleAsync(e => e.Id == scope.SystemAdmin.Id);
        Assert.Equal(EmployeeRole.SystemAdministrator, reloaded.Role);
    }

    [Fact]
    public async Task SystemAdministrator_DemoteNonLastAdmin_AllowedWhenAnotherActiveAdminRemains()
    {
        await using var scope = await AccessScopeFixture.CreateAsync();

        var result = await scope.Service.UpdateRoleAsync(
            scope.SystemAdminId,
            scope.SystemAdmin2.Id,
            new EmployeeRoleUpdateRequest { Role = EmployeeRole.Employee },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EmployeeRole.Employee, result.Value!.Role);

        scope.Environment.Context.ChangeTracker.Clear();
        var reloaded = await scope.Environment.Context.Employees.SingleAsync(e => e.Id == scope.SystemAdmin2.Id);
        Assert.Equal(EmployeeRole.Employee, reloaded.Role);
        var activeAdminCount = await scope.Environment.Context.Employees.CountAsync(e =>
            e.Role == EmployeeRole.SystemAdministrator
            && e.Status == EmployeeStatus.Active
            && !e.IsDeleted);
        Assert.Equal(1, activeAdminCount);
    }

    private sealed class AccessScopeFixture : IAsyncDisposable
    {
        private readonly SqliteTestEnvironment _environment;

        private AccessScopeFixture(
            SqliteTestEnvironment environment,
            Employee employee,
            Employee manager,
            Employee directReport,
            Employee indirectReport,
            Employee otherEmployee,
            Employee terminatedReport,
            Employee deletedReport,
            Employee noTeamManager,
            Employee hrAdmin,
            Employee systemAdmin,
            Employee systemAdmin2)
        {
            _environment = environment;
            Employee = employee;
            EmployeeId = employee.Id;
            Manager = manager;
            ManagerId = manager.Id;
            DirectReport = directReport;
            IndirectReport = indirectReport;
            OtherEmployee = otherEmployee;
            TerminatedReport = terminatedReport;
            DeletedReport = deletedReport;
            NoTeamManager = noTeamManager;
            NoTeamManagerId = noTeamManager.Id;
            HRAdmin = hrAdmin;
            HRAdminId = hrAdmin.Id;
            SystemAdmin = systemAdmin;
            SystemAdminId = systemAdmin.Id;
            SystemAdmin2 = systemAdmin2;
            DefaultDepartmentId = environment.DefaultDepartment!.Id;
            Service = environment.GetRequiredService<IEmployeeService>();
        }

        public SqliteTestEnvironment Environment => _environment;
        public IEmployeeService Service { get; }
        public Employee Employee { get; }
        public Guid EmployeeId { get; }
        public Employee Manager { get; }
        public Guid ManagerId { get; }
        public Employee DirectReport { get; }
        public Employee IndirectReport { get; }
        public Employee OtherEmployee { get; }
        public Employee TerminatedReport { get; }
        public Employee DeletedReport { get; }
        public Employee NoTeamManager { get; }
        public Guid NoTeamManagerId { get; }
        public Employee HRAdmin { get; }
        public Guid HRAdminId { get; }
        public Employee SystemAdmin { get; }
        public Guid SystemAdminId { get; }
        public Employee SystemAdmin2 { get; }
        public Guid DefaultDepartmentId { get; }

        public static async Task<AccessScopeFixture> CreateAsync()
        {
            var utcNow = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);
            var environment = await SqliteTestEnvironment.CreateAsync(
                seedDefaultDepartment: true,
                timeProvider: new TestTimeProvider(utcNow));
            var deptId = environment.DefaultDepartment!.Id;

            var admin = await environment.AddEmployeeAsync("ADMIN-SYS", "sysadmin@example.com", deptId, role: EmployeeRole.SystemAdministrator);
            var admin2 = await environment.AddEmployeeAsync("ADMIN-SYS2", "sysadmin2@example.com", deptId, role: EmployeeRole.SystemAdministrator);
            var hrAdmin = await environment.AddEmployeeAsync("ADMIN-HR", "hr@example.com", deptId, role: EmployeeRole.HRAdministrator);
            var manager = await environment.AddEmployeeAsync("MGR-001", "manager@example.com", deptId, role: EmployeeRole.Manager);
            var directReport = await environment.AddEmployeeAsync("EMP-DR", "direct@example.com", deptId, managerId: manager.Id);
            var indirectReport = await environment.AddEmployeeAsync("EMP-IR", "indirect@example.com", deptId, managerId: directReport.Id);
            var otherEmployee = await environment.AddEmployeeAsync("EMP-OTH", "other@example.com", deptId);
            var terminatedReport = await environment.AddEmployeeAsync("EMP-TERM", "term@example.com", deptId, managerId: manager.Id, status: EmployeeStatus.Terminated, terminatedAt: utcNow);
            var deletedReport = await environment.AddEmployeeAsync("EMP-DEL", "deleted@example.com", deptId, managerId: manager.Id, status: EmployeeStatus.Terminated, isDeleted: true, terminatedAt: utcNow);
            var noTeamManager = await environment.AddEmployeeAsync("MGR-NTM", "noteam@example.com", deptId, role: EmployeeRole.Manager);
            var employee = await environment.AddEmployeeAsync("EMP-001", "employee@example.com", deptId);

            return new AccessScopeFixture(
                environment, employee, manager, directReport, indirectReport,
                otherEmployee, terminatedReport, deletedReport, noTeamManager,
                hrAdmin, admin, admin2);
        }

        public static async Task<AccessScopeFixture> CreateSingleAdminAsync()
        {
            var utcNow = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);
            var environment = await SqliteTestEnvironment.CreateAsync(
                seedDefaultDepartment: true,
                timeProvider: new TestTimeProvider(utcNow));
            var deptId = environment.DefaultDepartment!.Id;

            var admin = await environment.AddEmployeeAsync("ADMIN-ONLY", "onlyadmin@example.com", deptId, role: EmployeeRole.SystemAdministrator);
            var hrAdmin = await environment.AddEmployeeAsync("ADMIN-HR2", "hr2@example.com", deptId, role: EmployeeRole.HRAdministrator);
            var manager = await environment.AddEmployeeAsync("MGR-002", "manager2@example.com", deptId, role: EmployeeRole.Manager);
            var directReport = await environment.AddEmployeeAsync("EMP-DR2", "direct2@example.com", deptId, managerId: manager.Id);
            var indirectReport = await environment.AddEmployeeAsync("EMP-IR2", "indirect2@example.com", deptId, managerId: directReport.Id);
            var otherEmployee = await environment.AddEmployeeAsync("EMP-OTH2", "other2@example.com", deptId);
            var terminatedReport = await environment.AddEmployeeAsync("EMP-TRM2", "term2@example.com", deptId, managerId: manager.Id, status: EmployeeStatus.Terminated, terminatedAt: utcNow);
            var deletedReport = await environment.AddEmployeeAsync("EMP-DEL2", "del2@example.com", deptId, managerId: manager.Id, status: EmployeeStatus.Terminated, isDeleted: true, terminatedAt: utcNow);
            var noTeamManager = await environment.AddEmployeeAsync("MGR-NT2", "noteam2@example.com", deptId, role: EmployeeRole.Manager);
            var employee = await environment.AddEmployeeAsync("EMP-002", "employee2@example.com", deptId);

            return new AccessScopeFixture(
                environment, employee, manager, directReport, indirectReport,
                otherEmployee, terminatedReport, deletedReport, noTeamManager,
                hrAdmin, admin, admin);
        }

        public ValueTask DisposeAsync()
        {
            return _environment.DisposeAsync();
        }
    }
}
