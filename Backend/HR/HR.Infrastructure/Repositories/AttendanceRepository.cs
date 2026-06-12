using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class AttendanceRepository(ApplicationDbContext context) : IAttendanceRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<AttendanceRecord?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken ct)
    {
        return _context.AttendanceRecords.FirstOrDefaultAsync(
            r => r.EmployeeId == employeeId && r.AttendanceDate == attendanceDate,
            ct);
    }

    public Task<AttendanceRecord?> GetOpenByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken ct)
    {
        return _context.AttendanceRecords.FirstOrDefaultAsync(
            r => r.EmployeeId == employeeId && r.AttendanceDate == attendanceDate && r.ClockOutAtUtc == null,
            ct);
    }

    public Task<PagedList<AttendanceRecord>> GetPageAsync(
        Guid? employeeId,
        IReadOnlySet<Guid>? employeeIds,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _context.AttendanceRecords.AsNoTracking().OrderByDescending(r => r.AttendanceDate).ThenBy(r => r.EmployeeId).AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (employeeIds is not null)
        {
            query = query.Where(r => employeeIds.Contains(r.EmployeeId));
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.AttendanceDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.AttendanceDate <= to.Value);
        }

        return PagedQueryExecutor.ExecuteAsync(query, page, pageSize, ct);
    }

    public Task AddAsync(AttendanceRecord record, CancellationToken ct)
    {
        return _context.AttendanceRecords.AddAsync(record, ct).AsTask();
    }
}
