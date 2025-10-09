namespace HR.Entities
{
    public class Position
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DepartmentId { get; set; }
        public string Title { get; set; } = default!;
        public string Code { get; set; } = default!;
        public int? Grade { get; set; }
        public bool IsManagerial { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public Department? Department { get; set; }
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
