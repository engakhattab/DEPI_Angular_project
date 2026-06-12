using HR.API.Extensions;
using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    private readonly IAttendanceService _attendanceService = attendanceService;

    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceRecordResponse>> ClockIn([FromBody] AttendanceClockInRequest request, CancellationToken ct)
    {
        var employeeId = User.GetEmployeeId();
        if (!employeeId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _attendanceService.ClockInAsync(employeeId.Value, request, ct);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceRecordResponse>> ClockOut([FromBody] AttendanceClockOutRequest request, CancellationToken ct)
    {
        var employeeId = User.GetEmployeeId();
        if (!employeeId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _attendanceService.ClockOutAsync(employeeId.Value, request, ct);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<AttendanceRecordResponse>>> GetAttendance(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _attendanceService.GetAttendanceAsync(
            requesterId.Value,
            new AttendanceQueryRequest { EmployeeId = employeeId, From = from, To = to, Page = page, PageSize = pageSize },
            ct);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }
}
