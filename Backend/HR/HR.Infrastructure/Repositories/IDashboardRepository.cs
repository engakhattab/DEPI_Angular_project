using HR.Domain.Enums;

namespace HR.Infrastructure.Repositories;

public interface IDashboardRepository
{
    Task<int> CountActiveEmployeesAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct);
    Task<int> CountDepartmentsAsync(CancellationToken ct);
    Task<int> CountPendingVacationRequestsAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct);
    Task<int> CountApprovedVacationsThisMonthAsync(IReadOnlySet<Guid>? employeeIds, DateOnly monthStart, DateOnly monthEnd, CancellationToken ct);
    Task<int> CountEmployeesOnVacationTodayAsync(IReadOnlySet<Guid>? employeeIds, DateOnly today, CancellationToken ct);
    Task<int> CountNewHiresThisMonthAsync(IReadOnlySet<Guid>? employeeIds, DateOnly monthStart, DateOnly monthEnd, CancellationToken ct);
    Task<int> CountUpcomingTripsThisWeekAsync(IReadOnlySet<Guid>? employeeIds, DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyDictionary<string, int>> CountEmployeesPerDepartmentAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct);
    Task<IReadOnlyDictionary<string, int>> CountVacationRequestsByStatusAsync(IReadOnlySet<Guid>? employeeIds, CancellationToken ct);
}
