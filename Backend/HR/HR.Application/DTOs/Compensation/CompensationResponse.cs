namespace HR.Application.DTOs.Compensation;

public class CompensationResponse
{
    public Guid EmployeeId { get; set; }
    public decimal? BaseSalary { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateOnly? LastSalaryReviewDate { get; set; }
    public IReadOnlyList<SalaryHistoryEntryResponse> History { get; set; } = [];
}
