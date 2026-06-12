using HR.Domain.Entities;
using HR.Shared.Pagination;

namespace HR.Infrastructure.Repositories;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken ct);
    Task<AttendanceRecord?> GetOpenByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken ct);
    Task<PagedList<AttendanceRecord>> GetPageAsync(Guid? employeeId, IReadOnlySet<Guid>? employeeIds, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task AddAsync(AttendanceRecord record, CancellationToken ct);
}
