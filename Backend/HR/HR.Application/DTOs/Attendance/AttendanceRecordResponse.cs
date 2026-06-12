namespace HR.Application.DTOs.Attendance;

public class AttendanceRecordResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset ClockInAtUtc { get; set; }
    public DateTimeOffset? ClockOutAtUtc { get; set; }
    public decimal? WorkedHours { get; set; }
    public string? Notes { get; set; }
}
