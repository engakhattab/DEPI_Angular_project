using HR.Domain.Enums;

namespace HR.Domain.Entities;

public class VacationRequest
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public VacationRequestStatus Status { get; set; } = VacationRequestStatus.Pending;

    public int WorkingDayCount { get; set; }

    public Guid? ReviewedByEmployeeId { get; set; }

    public Employee? ReviewedBy { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
