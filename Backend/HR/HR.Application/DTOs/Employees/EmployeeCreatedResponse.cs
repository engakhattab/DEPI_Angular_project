namespace HR.Application.DTOs.Employees;

public class EmployeeCreatedResponse
{
    public EmployeeResponse Employee { get; set; } = new();

    public string? TemporaryPassword { get; set; }
}
