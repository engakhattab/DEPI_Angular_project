namespace HR.Application.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    public int? TotalActiveEmployees { get; set; }
    public int? TotalDepartments { get; set; }
    public int? PendingVacationRequests { get; set; }
    public int? ApprovedVacationsThisMonth { get; set; }
    public int? EmployeesOnVacationToday { get; set; }
    public int? NewHiresThisMonth { get; set; }
    public int? UpcomingTripsThisWeek { get; set; }
    public IReadOnlyDictionary<string, int> EmployeesPerDepartment { get; set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> VacationRequestsByStatus { get; set; } = new Dictionary<string, int>();
}
