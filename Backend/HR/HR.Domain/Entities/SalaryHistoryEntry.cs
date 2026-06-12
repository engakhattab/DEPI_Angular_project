namespace HR.Domain.Entities;

public class SalaryHistoryEntry
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal? PreviousBaseSalary { get; set; }
    public decimal? NewBaseSalary { get; set; }
    public string? PreviousCurrency { get; set; }
    public string? NewCurrency { get; set; }
    public DateOnly? PreviousReviewDate { get; set; }
    public DateOnly? NewReviewDate { get; set; }
    public Guid ChangedByEmployeeId { get; set; }
    public Employee? ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}
