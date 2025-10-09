namespace HR.DTOs.LeaveRequests
{
    public record LeaveRequestDecisionDto(
    string Decision, // "Approve" or "Reject" or "Cancel"
    Guid? ApprovedBy
);
}
