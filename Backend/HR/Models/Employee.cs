namespace HR.Models;

public class Employee
{
    public Guid Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? ManagerId { get; set; }

    public Employee? Manager { get; set; }

    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    public DateOnly? BirthDate { get; set; }

    public DateOnly? JoinDate { get; set; }

    public string? JobTitle { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? IdentityUser { get; set; }
}
