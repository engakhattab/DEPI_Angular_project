using HR.Application.VacationRequests;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.VacationRequests;

public sealed class VacationRequestScopeFixture : IAsyncDisposable
{
    private readonly SqliteTestEnvironment _environment;

    private VacationRequestScopeFixture(SqliteTestEnvironment environment, DateTimeOffset utcNow)
    {
        _environment = environment;
        UtcNow = utcNow;
        Service = environment.GetRequiredService<IVacationRequestService>();
    }

    public SqliteTestEnvironment Environment => _environment;
    public IVacationRequestService Service { get; }
    public DateTimeOffset UtcNow { get; private set; }

    public Employee? Employee { get; private set; }
    public Employee? Manager { get; private set; }
    public Employee? DirectReport { get; private set; }
    public Employee? IndirectReport { get; private set; }
    public Employee? HrAdmin { get; private set; }
    public Employee? SysAdmin { get; private set; }
    public Employee? OtherEmployee { get; private set; }

    public VacationRequest? EmployeeOwnRequest { get; private set; }
    public VacationRequest? EmployeeOtherRequest { get; private set; }
    public VacationRequest? ManagerOwnRequest { get; private set; }
    public VacationRequest? DirectReportRequest { get; private set; }
    public VacationRequest? IndirectReportRequest { get; private set; }
    public VacationRequest? OtherEmployeeRequest { get; private set; }

    public static async Task<VacationRequestScopeFixture> CreateAsync()
    {
        var utcNow = new DateTimeOffset(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
        var environment = await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(utcNow));

        var fixture = new VacationRequestScopeFixture(environment, utcNow);
        await fixture.SeedAsync();
        return fixture;
    }

    private async Task SeedAsync()
    {
        var deptId = _environment.DefaultDepartment!.Id;

        Manager = await _environment.AddEmployeeAsync("MGR-01", "manager@test.com", deptId,
            role: EmployeeRole.Manager);
        DirectReport = await _environment.AddEmployeeAsync("DR-01", "direct@test.com", deptId,
            managerId: Manager.Id);
        IndirectReport = await _environment.AddEmployeeAsync("IR-01", "indirect@test.com", deptId,
            managerId: DirectReport.Id);
        Employee = await _environment.AddEmployeeAsync("EMP-01", "employee@test.com", deptId);
        HrAdmin = await _environment.AddEmployeeAsync("HR-01", "hr@test.com", deptId,
            role: EmployeeRole.HRAdministrator);
        SysAdmin = await _environment.AddEmployeeAsync("SYS-01", "sysadmin@test.com", deptId,
            role: EmployeeRole.SystemAdministrator);
        OtherEmployee = await _environment.AddEmployeeAsync("OTH-01", "other@test.com", deptId);

        var now = UtcNow;
        EmployeeOwnRequest = await _environment.AddVacationRequestAsync(Employee.Id, VacationRequestStatus.Pending, now, workingDayCount: 2);
        EmployeeOtherRequest = await _environment.AddVacationRequestAsync(OtherEmployee.Id, VacationRequestStatus.Pending, now, workingDayCount: 2);
        ManagerOwnRequest = await _environment.AddVacationRequestAsync(Manager.Id, VacationRequestStatus.Pending, now, workingDayCount: 2);
        DirectReportRequest = await _environment.AddVacationRequestAsync(DirectReport.Id, VacationRequestStatus.Pending, now, workingDayCount: 2);
        IndirectReportRequest = await _environment.AddVacationRequestAsync(IndirectReport.Id, VacationRequestStatus.Pending, now, workingDayCount: 2);
        OtherEmployeeRequest = EmployeeOtherRequest;
    }

    public ValueTask DisposeAsync()
    {
        return _environment.DisposeAsync();
    }
}
