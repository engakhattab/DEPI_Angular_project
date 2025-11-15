using System.ComponentModel.DataAnnotations;

namespace HR.DTOs.VacationRequests;

public class VacationRequestCreateRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
