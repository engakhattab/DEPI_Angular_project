using Microsoft.EntityFrameworkCore;

namespace HR.Entities
{
    public class LeaveType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public bool IsPaid { get; set; } = true;
        [Precision(5, 2)]
        public decimal? MaxPerYearDays { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
