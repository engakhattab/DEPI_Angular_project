using HR.Data;
using HR.DTOs.Employyes;
using HR.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly HrDbContext _db;
        public EmployeesController(HrDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> Get([FromQuery] Guid? deptId, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Employees.AsNoTracking().Where(e => e.IsActive);
            if (deptId.HasValue) query = query.Where(e => e.DepartmentId == deptId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(e => e.EmployeeNumber.ToLower().Contains(term) || e.FirstName.ToLower().Contains(term) || e.LastName.ToLower().Contains(term) || (e.Email != null && e.Email.ToLower().Contains(term)));
            }
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Employee>> GetById(Guid id)
        {
            var emp = await _db.Employees.FindAsync(id);
            return emp is null ? NotFound() : Ok(emp);
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> Create([FromBody] EmployeeCreateDto dto)
        {
            // Basic validation
            if (await _db.Employees.AnyAsync(e => e.EmployeeNumber == dto.EmployeeNumber))
                return Conflict($"EmployeeNumber '{dto.EmployeeNumber}' already exists.");

            var employee = new Employee
            {
                EmployeeNumber = dto.EmployeeNumber,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                HireDate = dto.HireDate,
                DepartmentId = dto.DepartmentId,
                PositionId = dto.PositionId,
                ManagerId = dto.ManagerId
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] EmployeeUpdateDto dto)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp is null) return NotFound();

            emp.FirstName = dto.FirstName;
            emp.LastName = dto.LastName;
            emp.Email = dto.Email;
            emp.Phone = dto.Phone;
            emp.DepartmentId = dto.DepartmentId;
            emp.PositionId = dto.PositionId;
            emp.ManagerId = dto.ManagerId;
            emp.IsActive = dto.IsActive;
            emp.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp is null) return NotFound();
            _db.Employees.Remove(emp);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
