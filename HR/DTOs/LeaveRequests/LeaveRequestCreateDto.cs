namespace HR.DTOs.LeaveRequests
{
    public record LeaveRequestCreateDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    decimal Days,
    string? Reason
);
}
