using HR.Application.Authorization;
using HR.Application.Dashboard;
using HR.Application.DTOs.Dashboard;
using HR.Domain.Enums;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.Repositories;
using HR.Shared.Results;

namespace HR.Infrastructure.Dashboard;

public class DashboardService(
    IEmployeeAccessService accessService,
    IDashboardRepository dashboardRepository,
    IBusinessTimeProvider businessTimeProvider) : IDashboardService
{
    private readonly IEmployeeAccessService _accessService = accessService;
    private readonly IDashboardRepository _dashboardRepository = dashboardRepository;
    private readonly IBusinessTimeProvider _businessTimeProvider = businessTimeProvider;

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(Guid requesterEmployeeId, CancellationToken ct)
    {
        var access = await _accessService.GetCurrentAsync(requesterEmployeeId, ct);
        if (access.IsFailure)
        {
            return Result<DashboardSummaryResponse>.Failure(access.Error!);
        }

        if (access.Value!.Role == EmployeeRole.Employee)
        {
            return Result<DashboardSummaryResponse>.Failure(ServiceError.Forbidden());
        }

        IReadOnlySet<Guid>? employeeIds = null;
        var managerScope = access.Value.Role == EmployeeRole.Manager;
        if (managerScope)
        {
            employeeIds = (await _accessService.GetVisibleEmployeeIdsAsync(requesterEmployeeId, ct))
                .Where(id => id != requesterEmployeeId)
                .ToHashSet();
        }

        var today = _businessTimeProvider.GetBusinessDate(_businessTimeProvider.GetUtcNow());
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var weekEnd = today.AddDays(7);

        return Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse
        {
            TotalActiveEmployees = await _dashboardRepository.CountActiveEmployeesAsync(employeeIds, ct),
            TotalDepartments = managerScope ? null : await _dashboardRepository.CountDepartmentsAsync(ct),
            PendingVacationRequests = await _dashboardRepository.CountPendingVacationRequestsAsync(employeeIds, ct),
            ApprovedVacationsThisMonth = await _dashboardRepository.CountApprovedVacationsThisMonthAsync(employeeIds, monthStart, monthEnd, ct),
            EmployeesOnVacationToday = await _dashboardRepository.CountEmployeesOnVacationTodayAsync(employeeIds, today, ct),
            NewHiresThisMonth = await _dashboardRepository.CountNewHiresThisMonthAsync(employeeIds, monthStart, monthEnd, ct),
            UpcomingTripsThisWeek = await _dashboardRepository.CountUpcomingTripsThisWeekAsync(employeeIds, today, weekEnd, ct),
            EmployeesPerDepartment = await _dashboardRepository.CountEmployeesPerDepartmentAsync(employeeIds, ct),
            VacationRequestsByStatus = await _dashboardRepository.CountVacationRequestsByStatusAsync(employeeIds, ct)
        });
    }
}
