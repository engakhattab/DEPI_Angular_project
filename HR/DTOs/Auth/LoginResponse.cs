using HR.DTOs.Employees;

namespace HR.DTOs.Auth;

public class LoginResponse
{
    public EmployeeResponse Employee { get; set; } = new();
}
