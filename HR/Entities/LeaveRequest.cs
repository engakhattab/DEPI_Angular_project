using Microsoft.EntityFrameworkCore;
namespace HR.Entities
{
    public class LeaveRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Precision(5, 2)]
        public decimal Days { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected/Cancelled
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public Employee? Employee { get; set; }
        public LeaveType? LeaveType { get; set; }
    }

}
