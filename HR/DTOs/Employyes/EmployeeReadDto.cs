namespace HR.DTOs.Employyes
{
    public class EmployeeReadDto
    {
        public Guid Id { get; set; }
        public string EmployeeNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime HireDate { get; set; }               // Join Date
        public DateTime? BirthDate { get; set; }             // NEW
        public string? Notes { get; set; }                   // NEW
        public bool IsActive { get; set; }
    }

}
