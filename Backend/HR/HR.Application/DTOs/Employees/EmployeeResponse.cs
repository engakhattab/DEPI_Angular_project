using HR.Domain.Enums;

namespace HR.Application.DTOs.Employees;

public class EmployeeResponse
{
    public Guid Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public Guid? ManagerId { get; set; }

    public string? ManagerName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? JoinDate { get; set; }

    public string? JobTitle { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public int VacationBalanceDays { get; set; } = 21;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? TerminatedAt { get; set; }

    public string IdentityUserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
