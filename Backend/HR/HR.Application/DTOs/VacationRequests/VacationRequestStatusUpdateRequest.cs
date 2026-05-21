using System.ComponentModel.DataAnnotations;
using HR.Domain.Enums;

namespace HR.Application.DTOs.VacationRequests;

public class VacationRequestStatusUpdateRequest
{
    [Required]
    public VacationRequestStatus Status { get; set; }
}
