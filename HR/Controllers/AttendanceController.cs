using HR.Data;
using HR.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly HrDbContext _db;
        public AttendanceController(HrDbContext db) => _db = db;

        [HttpPost("clock-in/{employeeId:guid}")]
        public async Task<ActionResult> ClockIn(Guid employeeId)
        {
            var emp = await _db.Employees.FindAsync(employeeId);
            if (emp is null) return NotFound("Employee not found.");

            var today = DateTime.UtcNow.Date;
            var existing = await _db.AttendanceRecords.SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today);
            if (existing is not null) return Conflict("Already clocked in today.");

            var record = new AttendanceRecord
            {
                EmployeeId = employeeId,
                WorkDate = today,
                CheckIn = DateTime.UtcNow,
                Status = "Present"
            };
            _db.AttendanceRecords.Add(record);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { employeeId, from = today, to = today }, record);
        }

        [HttpPost("clock-out/{employeeId:guid}")]
        public async Task<ActionResult> ClockOut(Guid employeeId)
        {
            var today = DateTime.UtcNow.Date;
            var record = await _db.AttendanceRecords.SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today);
            if (record is null) return NotFound("No clock-in record for today.");
            if (record.CheckOut is not null) return Conflict("Already clocked out.");

            record.CheckOut = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceRecord>>> Get([FromQuery] Guid? employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var q = _db.AttendanceRecords.AsNoTracking().AsQueryable();
            if (employeeId.HasValue) q = q.Where(a => a.EmployeeId == employeeId.Value);
            if (from.HasValue) q = q.Where(a => a.WorkDate >= from.Value.Date);
            if (to.HasValue) q = q.Where(a => a.WorkDate <= to.Value.Date);
            var list = await q.OrderByDescending(a => a.WorkDate).ToListAsync();
            return Ok(list);
        }
    }

}
