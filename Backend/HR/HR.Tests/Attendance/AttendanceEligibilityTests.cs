using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Attendance;

public class AttendanceEligibilityTests
{
    [Theory]
    [InlineData(EmployeeStatus.Suspended, false)]
    [InlineData(EmployeeStatus.Terminated, false)]
    [InlineData(EmployeeStatus.Active, true)]
    public async Task ClockInAsync_WhenEmployeeIsNotEligible_ReturnsForbidden(EmployeeStatus status, bool isDeleted)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync(
            $"ATT-ELIG-{Guid.NewGuid():N}"[..18],
            $"attendance-elig-{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            status: status,
            isDeleted: isDeleted,
            role: EmployeeRole.Employee);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockInAsync(employee.Id, new AttendanceClockInRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(await environment.Context.AttendanceRecords.ToListAsync());
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Fact]
    public async Task ClockInAsync_WhenEmployeeIsMissing_ReturnsUnauthorized()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockInAsync(Guid.NewGuid(), new AttendanceClockInRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(await environment.Context.AttendanceRecords.ToListAsync());
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Fact]
    public async Task ClockOutAsync_WhenEmployeeIsNotEligible_ReturnsForbidden()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync(
            "ATT-ELIG-OUT",
            "attendance-elig-out@example.com",
            environment.DefaultDepartment!.Id,
            status: EmployeeStatus.Suspended);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockOutAsync(employee.Id, new AttendanceClockOutRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }
}
