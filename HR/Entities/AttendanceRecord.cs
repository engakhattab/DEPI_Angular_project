namespace HR.Entities
{
    public class AttendanceRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EmployeeId { get; set; }
        public DateTime WorkDate { get; set; } // UTC Date
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string Status { get; set; } = "Present"; // Present/Late/Absent/OnLeave
        public int OvertimeMinutes { get; set; } = 0;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Employee? Employee { get; set; }
    }
}
