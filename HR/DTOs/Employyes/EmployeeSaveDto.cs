namespace HR.DTOs.Employyes
{
    public class EmployeeSaveDto
    {
        public string EmployeeNumber { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }                 // allow frontend to send just FullName
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? PositionId { get; set; }                 // job title by id
        public string? JobTitle { get; set; }                 // OR job title by name (optional)
        public Guid? ManagerId { get; set; }
        public DateTime HireDate { get; set; }                // Join Date
        public DateTime? BirthDate { get; set; }              // NEW
        public string? Notes { get; set; }                    // NEW
        public bool IsActive { get; set; } = true;
    }
}
