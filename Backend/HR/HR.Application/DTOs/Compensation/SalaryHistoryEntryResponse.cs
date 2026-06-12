namespace HR.Application.DTOs.Compensation;

public class SalaryHistoryEntryResponse
{
    public Guid Id { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public Guid ChangedByEmployeeId { get; set; }
    public decimal? PreviousBaseSalary { get; set; }
    public decimal? NewBaseSalary { get; set; }
    public string? PreviousCurrency { get; set; }
    public string? NewCurrency { get; set; }
    public DateOnly? PreviousReviewDate { get; set; }
    public DateOnly? NewReviewDate { get; set; }
}
