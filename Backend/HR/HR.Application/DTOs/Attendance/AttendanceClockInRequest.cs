using System.ComponentModel.DataAnnotations;

namespace HR.Application.DTOs.Attendance;

public class AttendanceClockInRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}
