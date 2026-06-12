using HR.Domain.Enums;

namespace HR.Application.DTOs.Employees;

public class EmployeeRoleResponse
{
    public Guid EmployeeId { get; set; }
    public EmployeeRole Role { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
