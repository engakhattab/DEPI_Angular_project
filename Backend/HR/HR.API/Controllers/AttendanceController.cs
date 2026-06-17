using HR.API.Documentation;
using HR.API.Extensions;
using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Produces("application/json")]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    private readonly IAttendanceService _attendanceService = attendanceService;

    [HttpPost("clock-in")]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
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
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
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
    [ProducesResponseType(typeof(PagedList<AttendanceRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
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
