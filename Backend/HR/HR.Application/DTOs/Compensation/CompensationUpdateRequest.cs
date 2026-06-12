namespace HR.Application.DTOs.Compensation;

public class CompensationUpdateRequest
{
    public decimal BaseSalary { get; set; }
    public string SalaryCurrency { get; set; } = "EGP";
    public DateOnly? LastSalaryReviewDate { get; set; }
}
