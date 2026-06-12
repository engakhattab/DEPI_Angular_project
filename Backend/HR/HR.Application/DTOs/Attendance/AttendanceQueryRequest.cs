namespace HR.Application.DTOs.Attendance;

public class AttendanceQueryRequest
{
    public Guid? EmployeeId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
