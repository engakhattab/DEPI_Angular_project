using System.ComponentModel.DataAnnotations;

namespace HR.Application.DTOs.Attendance;

public class AttendanceClockOutRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}
