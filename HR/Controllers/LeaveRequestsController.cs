using HR.Data;
using HR.DTOs.LeaveRequests;
using HR.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly HrDbContext _db;
        public LeaveRequestsController(HrDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveRequest>>> Get([FromQuery] string? status, [FromQuery] Guid? empId)
        {
            var q = _db.LeaveRequests.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToLower();
                q = q.Where(l => l.Status.ToLower() == s);
            }
            if (empId.HasValue) q = q.Where(l => l.EmployeeId == empId.Value);
            var list = await q.OrderByDescending(l => l.RequestedAt).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<LeaveRequest>> Create([FromBody] LeaveRequestCreateDto dto)
        {
            if (dto.EndDate < dto.StartDate) return BadRequest("EndDate must be after StartDate.");

            var emp = await _db.Employees.FindAsync(dto.EmployeeId);
            if (emp is null) return NotFound("Employee not found.");
            var type = await _db.LeaveTypes.FindAsync(dto.LeaveTypeId);
            if (type is null) return NotFound("LeaveType not found.");

            var lr = new LeaveRequest
            {
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Days = dto.Days,
                Reason = dto.Reason
            };
            _db.LeaveRequests.Add(lr);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = lr.Id }, lr);
        }

        [HttpPut("{id:guid}/decision")]
        public async Task<ActionResult> Decide(Guid id, [FromBody] LeaveRequestDecisionDto dto)
        {
            var lr = await _db.LeaveRequests.FindAsync(id);
            if (lr is null) return NotFound();

            var decision = dto.Decision?.Trim().ToLower();
            if (decision == "approve")
            {
                lr.Status = "Approved";
                lr.ApprovedBy = dto.ApprovedBy;
                lr.ApprovedAt = DateTime.UtcNow;
            }
            else if (decision == "reject")
            {
                lr.Status = "Rejected";
                lr.ApprovedBy = dto.ApprovedBy;
                lr.ApprovedAt = DateTime.UtcNow;
            }
            else if (decision == "cancel")
            {
                lr.Status = "Cancelled";
            }
            else
            {
                return BadRequest("Decision must be Approve, Reject or Cancel.");
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
