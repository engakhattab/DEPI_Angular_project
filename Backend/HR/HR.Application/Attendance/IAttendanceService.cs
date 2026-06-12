using HR.Application.DTOs.Attendance;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Attendance;

public interface IAttendanceService
{
    Task<Result<AttendanceRecordResponse>> ClockInAsync(Guid employeeId, AttendanceClockInRequest request, CancellationToken ct);
    Task<Result<AttendanceRecordResponse>> ClockOutAsync(Guid employeeId, AttendanceClockOutRequest request, CancellationToken ct);
    Task<Result<PagedList<AttendanceRecordResponse>>> GetAttendanceAsync(Guid requesterEmployeeId, AttendanceQueryRequest request, CancellationToken ct);
}
