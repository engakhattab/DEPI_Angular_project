using HR.Domain.Entities;
using HR.Domain.Enums;

namespace HR.Application.DTOs.VacationRequests;

public class VacationRequestResponse
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public VacationRequestStatus Status { get; set; }

    public int WorkingDayCount { get; set; }

    public Guid? ReviewedByEmployeeId { get; set; }

    public string? ReviewedByEmployeeName { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public static VacationRequestResponse FromEntity(VacationRequest request)
    {
        return new VacationRequestResponse
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.Employee?.FullName ?? string.Empty,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = request.Status,
            WorkingDayCount = request.WorkingDayCount,
            ReviewedByEmployeeId = request.ReviewedByEmployeeId,
            ReviewedByEmployeeName = request.ReviewedBy?.FullName,
            ReviewedAt = request.ReviewedAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }
}
