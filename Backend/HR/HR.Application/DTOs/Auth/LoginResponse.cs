using HR.Application.DTOs.Employees;

namespace HR.Application.DTOs.Auth;

public class LoginResponse
{
    public EmployeeResponse Employee { get; set; } = new();
}
