using System.ComponentModel.DataAnnotations;

namespace HR.DTOs.Departments;

public class DepartmentUpdateRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
