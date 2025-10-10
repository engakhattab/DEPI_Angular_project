using System.ComponentModel.DataAnnotations;

namespace HR.DTOs.Departments;

public class DepartmentCreateRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
