using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Attendance;

public class AttendanceServiceTests
{
    [Fact]
    public async Task ClockInAsync_WhenActiveEmployeeHasNoRecord_CreatesAttendanceAndAuditEntry()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 6, 30, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var employee = await environment.AddEmployeeAsync("ATT-SVC-001", "attendance-service-1@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockInAsync(employee.Id, new AttendanceClockInRequest { Notes = "Morning shift" }, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(employee.Id, result.Value!.EmployeeId);
        Assert.Equal(new DateOnly(2026, 6, 7), result.Value.AttendanceDate);
        Assert.Equal(clock.GetUtcNow(), result.Value.ClockInAtUtc);
        Assert.Null(result.Value.ClockOutAtUtc);
        Assert.Null(result.Value.WorkedHours);
        Assert.Equal("Morning shift", result.Value.Notes);

        var record = await environment.Context.AttendanceRecords.SingleAsync();
        Assert.Equal(employee.Id, record.EmployeeId);
        Assert.Equal(new DateOnly(2026, 6, 7), record.AttendanceDate);
        Assert.Equal(clock.GetUtcNow(), record.ClockInAtUtc);
        var audit = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionType.ClockedIn, audit.ActionType);
        Assert.Equal(employee.Id, audit.ActorEmployeeId);
    }

    [Fact]
    public async Task ClockInAsync_WhenRecordAlreadyExistsForBusinessDate_ReturnsConflict()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 6, 30, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var employee = await environment.AddEmployeeAsync("ATT-SVC-002", "attendance-service-2@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IAttendanceService>();
        await service.ClockInAsync(employee.Id, new AttendanceClockInRequest(), CancellationToken.None);

        clock.SetUtcNow(new DateTimeOffset(2026, 6, 7, 9, 0, 0, TimeSpan.Zero));
        var duplicate = await service.ClockInAsync(employee.Id, new AttendanceClockInRequest(), CancellationToken.None);

        Assert.True(duplicate.IsFailure);
        Assert.Equal("CONFLICT", duplicate.Error!.Code);
        Assert.Equal(1, await environment.Context.AttendanceRecords.CountAsync());
        Assert.Equal(1, await environment.Context.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task ClockOutAsync_WhenOpenRecordExists_CompletesRecordAndCalculatesWorkedHours()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 6, 0, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var employee = await environment.AddEmployeeAsync("ATT-SVC-003", "attendance-service-3@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IAttendanceService>();
        var clockIn = await service.ClockInAsync(employee.Id, new AttendanceClockInRequest { Notes = "Start" }, CancellationToken.None);
        clock.SetUtcNow(new DateTimeOffset(2026, 6, 7, 14, 30, 0, TimeSpan.Zero));

        var clockOut = await service.ClockOutAsync(employee.Id, new AttendanceClockOutRequest { Notes = "End" }, CancellationToken.None);

        Assert.True(clockIn.IsSuccess, clockIn.Error?.Message);
        Assert.True(clockOut.IsSuccess, clockOut.Error?.Message);
        Assert.Equal(clock.GetUtcNow(), clockOut.Value!.ClockOutAtUtc);
        Assert.Equal(8.5m, clockOut.Value.WorkedHours);
        Assert.Equal("End", clockOut.Value.Notes);

        var record = await environment.Context.AttendanceRecords.SingleAsync();
        Assert.Equal(clock.GetUtcNow(), record.ClockOutAtUtc);
        Assert.Equal(clock.GetUtcNow(), record.UpdatedAt);
        Assert.Equal(2, await environment.Context.AuditLogEntries.CountAsync());
        Assert.Contains(await environment.Context.AuditLogEntries.ToListAsync(), a => a.ActionType == AuditActionType.ClockedOut);
    }

    [Fact]
    public async Task ClockOutAsync_WhenNoOpenRecordExists_ReturnsNotFound()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 6, 7, 14, 30, 0, TimeSpan.Zero));
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var employee = await environment.AddEmployeeAsync("ATT-SVC-004", "attendance-service-4@example.com", environment.DefaultDepartment!.Id);
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockOutAsync(employee.Id, new AttendanceClockOutRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        Assert.Empty(await environment.Context.AttendanceRecords.ToListAsync());
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ClockOutAsync_WhenClockOutIsBeforeOrEqualClockIn_ReturnsBusinessRule(int clockInMinutesAfterNow)
    {
        var now = new DateTimeOffset(2026, 6, 7, 14, 30, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(now);
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: clock);
        var employee = await environment.AddEmployeeAsync(
            $"ATT-SVC-{Guid.NewGuid():N}"[..15],
            $"attendance-{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id);
        environment.Context.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = employee.Id,
            AttendanceDate = new DateOnly(2026, 6, 7),
            ClockInAtUtc = now.AddMinutes(clockInMinutesAfterNow),
            CreatedAt = now.AddMinutes(clockInMinutesAfterNow)
        });
        await environment.Context.SaveChangesAsync();
        var service = environment.GetRequiredService<IAttendanceService>();

        var result = await service.ClockOutAsync(employee.Id, new AttendanceClockOutRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(await environment.Context.AuditLogEntries.ToListAsync());
    }
}
