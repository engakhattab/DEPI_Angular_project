using HR.Application.Attendance;
using HR.Application.Authorization;
using HR.Application.DTOs.Attendance;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Attendance;

public class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IEmployeeAccessService accessService,
    IBusinessTimeProvider businessTimeProvider,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork) : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
    private readonly IEmployeeAccessService _accessService = accessService;
    private readonly IBusinessTimeProvider _businessTimeProvider = businessTimeProvider;
    private readonly IAuditWriter _auditWriter = auditWriter;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<AttendanceRecordResponse>> ClockInAsync(Guid employeeId, AttendanceClockInRequest request, CancellationToken ct)
    {
        var access = await RequireActiveEmployeeAsync(employeeId, ct);
        if (access.IsFailure)
        {
            return Result<AttendanceRecordResponse>.Failure(access.Error!);
        }

        var now = _businessTimeProvider.GetUtcNow();
        var businessDate = _businessTimeProvider.GetBusinessDate(now);
        if (await _attendanceRepository.GetByEmployeeAndDateAsync(employeeId, businessDate, ct) is not null)
        {
            return Result<AttendanceRecordResponse>.Failure(ServiceError.Conflict("Attendance has already been recorded for this business date."));
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            AttendanceDate = businessDate,
            ClockInAtUtc = now,
            Notes = request.Notes,
            CreatedAt = now
        };

        await _attendanceRepository.AddAsync(record, ct);
        await _auditWriter.WriteAsync("AttendanceRecord", record.Id, AuditActionType.ClockedIn, employeeId, null, ["ClockInAtUtc"], null, new { clockInAtUtc = now }, null, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AttendanceRecordResponse>.Success(Map(record));
    }

    public async Task<Result<AttendanceRecordResponse>> ClockOutAsync(Guid employeeId, AttendanceClockOutRequest request, CancellationToken ct)
    {
        var access = await RequireActiveEmployeeAsync(employeeId, ct);
        if (access.IsFailure)
        {
            return Result<AttendanceRecordResponse>.Failure(access.Error!);
        }

        var now = _businessTimeProvider.GetUtcNow();
        var businessDate = _businessTimeProvider.GetBusinessDate(now);
        var record = await _attendanceRepository.GetOpenByEmployeeAndDateAsync(employeeId, businessDate, ct);
        if (record is null)
        {
            return Result<AttendanceRecordResponse>.Failure(ServiceError.NotFound("No open attendance record was found for this business date."));
        }

        if (now <= record.ClockInAtUtc)
        {
            return Result<AttendanceRecordResponse>.Failure(ServiceError.BusinessRule("Clock-out must be after clock-in."));
        }

        record.ClockOutAtUtc = now;
        record.UpdatedAt = now;
        record.Notes = string.IsNullOrWhiteSpace(request.Notes) ? record.Notes : request.Notes;

        await _auditWriter.WriteAsync("AttendanceRecord", record.Id, AuditActionType.ClockedOut, employeeId, null, ["ClockOutAtUtc"], null, new { clockOutAtUtc = now }, null, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AttendanceRecordResponse>.Success(Map(record));
    }

    public async Task<Result<PagedList<AttendanceRecordResponse>>> GetAttendanceAsync(Guid requesterEmployeeId, AttendanceQueryRequest request, CancellationToken ct)
    {
        var access = await _accessService.GetCurrentAsync(requesterEmployeeId, ct);
        if (access.IsFailure)
        {
            return Result<PagedList<AttendanceRecordResponse>>.Failure(access.Error!);
        }

        var requestedEmployeeId = request.EmployeeId;
        if (requestedEmployeeId.HasValue && !await _accessService.CanAccessEmployeeAsync(requesterEmployeeId, requestedEmployeeId.Value, ct))
        {
            return Result<PagedList<AttendanceRecordResponse>>.Failure(ServiceError.Forbidden());
        }

        IReadOnlySet<Guid>? visibleIds = null;
        if (!requestedEmployeeId.HasValue && access.Value!.Role is not EmployeeRole.HRAdministrator and not EmployeeRole.SystemAdministrator)
        {
            visibleIds = await _accessService.GetVisibleEmployeeIdsAsync(requesterEmployeeId, ct);
        }

        var page = await _attendanceRepository.GetPageAsync(requestedEmployeeId, visibleIds, request.From, request.To, request.Page, request.PageSize, ct);
        return Result<PagedList<AttendanceRecordResponse>>.Success(
            new PagedList<AttendanceRecordResponse>(page.Items.Select(Map).ToList(), page.TotalCount, page.Page, page.PageSize));
    }

    private async Task<Result<EmployeeAccessContext>> RequireActiveEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        var access = await _accessService.GetCurrentAsync(employeeId, ct);
        if (access.IsFailure)
        {
            return access;
        }

        if (access.Value!.IsDeleted || access.Value.IsTerminated || !access.Value.IsActive)
        {
            return Result<EmployeeAccessContext>.Failure(ServiceError.Forbidden("Employee is not eligible to record attendance."));
        }

        return access;
    }

    private static AttendanceRecordResponse Map(AttendanceRecord record)
    {
        return new AttendanceRecordResponse
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            AttendanceDate = record.AttendanceDate,
            ClockInAtUtc = record.ClockInAtUtc,
            ClockOutAtUtc = record.ClockOutAtUtc,
            WorkedHours = record.ClockOutAtUtc.HasValue
                ? Math.Round((decimal)(record.ClockOutAtUtc.Value - record.ClockInAtUtc).TotalHours, 2)
                : null,
            Notes = record.Notes
        };
    }
}
