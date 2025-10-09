namespace HR.DTOs.Employyes
{
    public class EmployeeListItemDto
    {
        public Guid Id { get; set; }
        public string EmployeeId { get; set; } = default!;   // EmployeeNumber
        public string Name { get; set; } = default!;         // FirstName + LastName
        public string? Department { get; set; }
        public string? Title { get; set; }                   // Position.Name
        public string? Manager { get; set; }                 // Manager full name
        public string Status { get; set; } = "Active";       // Active | Suspended
        public DateTime JoinDate { get; set; }               // HireDate
    }
}
