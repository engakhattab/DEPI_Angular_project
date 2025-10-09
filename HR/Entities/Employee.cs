namespace HR.Entities
{
    // Entities/Employee.cs
     using System.Collections.Generic;

    public class Employee
    {
        public Guid Id { get; set; }
        public string EmployeeNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Email { get; set; }
        public string? Phone { get; set; }

        // Join date
        public DateTime HireDate { get; set; }

        // Newly added earlier
        public DateTime? BirthDate { get; set; }
        public string? Notes { get; set; }

        // Department / Position
        public Guid DepartmentId { get; set; }
        public Department? Department { get; set; }
        public Guid? PositionId { get; set; }
        public Position? Position { get; set; }

        // Manager (self-reference)
        public Guid? ManagerId { get; set; }
        public Employee? Manager { get; set; }

        // 🔴 Add these two missing navigation collections:
        public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }


}
