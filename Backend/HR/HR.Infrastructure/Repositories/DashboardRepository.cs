using HR.Domain.Enums;
using HR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class DashboardRepository(ApplicationDbContext context) : IDashboardRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<int> CountActiveEmployeesAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct)
    {
        return EmployeeScope(employeeIds).CountAsync(e => e.Status == EmployeeStatus.Active, ct);
    }

    public Task<int> CountDepartmentsAsync(CancellationToken ct)
    {
        return _context.Departments.CountAsync(ct);
    }

    public Task<int> CountPendingVacationRequestsAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct)
    {
        return VacationScope(employeeIds).CountAsync(v => v.Status == VacationRequestStatus.Pending, ct);
    }

    public Task<int> CountApprovedVacationsThisMonthAsync(IReadOnlySet<Guid>? employeeIds, DateOnly monthStart, DateOnly monthEnd, CancellationToken ct)
    {
        return VacationScope(employeeIds).CountAsync(v => v.Status == VacationRequestStatus.Approved && v.StartDate <= monthEnd && v.EndDate >= monthStart, ct);
    }

    public Task<int> CountEmployeesOnVacationTodayAsync(IReadOnlySet<Guid>? employeeIds, DateOnly today, CancellationToken ct)
    {
        return VacationScope(employeeIds).CountAsync(v => v.Status == VacationRequestStatus.Approved && v.StartDate <= today && v.EndDate >= today, ct);
    }

    public Task<int> CountNewHiresThisMonthAsync(IReadOnlySet<Guid>? employeeIds, DateOnly monthStart, DateOnly monthEnd, CancellationToken ct)
    {
        return EmployeeScope(employeeIds).CountAsync(e => e.JoinDate >= monthStart && e.JoinDate <= monthEnd, ct);
    }

    public Task<int> CountUpcomingTripsThisWeekAsync(IReadOnlySet<Guid>? employeeIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var query = _context.Trips.AsNoTracking().Where(t => t.TripDate >= from && t.TripDate <= to);
        if (employeeIds is not null)
        {
            query = query.Where(t => t.RequestedByEmployeeId.HasValue && employeeIds.Contains(t.RequestedByEmployeeId.Value));
        }

        return query.CountAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountEmployeesPerDepartmentAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct)
    {
        return await EmployeeScope(employeeIds)
            .Where(e => e.Status == EmployeeStatus.Active)
            .GroupBy(e => e.Department!.Name)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Department, x => x.Count, ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountVacationRequestsByStatusAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct)
    {
        return await VacationScope(employeeIds)
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    private IQueryable<HR.Domain.Entities.Employee> EmployeeScope(IReadOnlySet<Guid>? employeeIds)
    {
        var query = _context.Employees.AsNoTracking().Include(e => e.Department).Where(e => !e.IsDeleted);
        return employeeIds is null ? query : query.Where(e => employeeIds.Contains(e.Id));
    }

    private IQueryable<HR.Domain.Entities.VacationRequest> VacationScope(IReadOnlySet<Guid>? employeeIds)
    {
        var query = _context.VacationRequests.AsNoTracking().Where(v => !v.Employee!.IsDeleted);
        return employeeIds is null ? query : query.Where(v => employeeIds.Contains(v.EmployeeId));
    }
}
