using System.ComponentModel.DataAnnotations;
using HR.Models;

namespace HR.DTOs.VacationRequests;

public class VacationRequestStatusUpdateRequest
{
    [Required]
    public VacationRequestStatus Status { get; set; }
}
