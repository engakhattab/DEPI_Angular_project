namespace HR.Domain.Entities;

public class EmployeeCompensation
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal BaseSalary { get; set; }
    public string SalaryCurrency { get; set; } = "EGP";
    public DateOnly? LastSalaryReviewDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
