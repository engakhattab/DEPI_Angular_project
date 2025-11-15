using System.ComponentModel.DataAnnotations;
using HR.Models;

namespace HR.DTOs.Employees;

public class EmployeeCreateRequest
{
    [Required]
    [StringLength(20)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    public Guid? ManagerId { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? JoinDate { get; set; }

    [StringLength(150)]
    public string? JobTitle { get; set; }

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    [StringLength(100)]
    public string? InitialPassword { get; set; }
}
