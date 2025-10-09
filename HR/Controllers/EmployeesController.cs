using HR.Data;
using HR.DTOs.Employyes;
using HR.Entities;
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

        // ====== GRID: matches the frontend table ======
        // GET /api/employees?q=&page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeListItemDto>>> Get(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var baseQuery = _db.Employees.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQuery = baseQuery.Where(e =>
                    e.EmployeeNumber.ToLower().Contains(term) ||
                    (e.FirstName + " " + e.LastName).ToLower().Contains(term));
            }

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    EmployeeId = e.EmployeeNumber,
                    Name = (e.FirstName + " " + e.LastName).Trim(),
                    Department = e.Department != null ? e.Department.Name : null,
                    // 🔁 CHANGED: Position.Title instead of Name
                    Title = e.Position != null ? e.Position.Title : null,
                    Manager = e.Manager != null ? (e.Manager.FirstName + " " + e.Manager.LastName).Trim() : null,
                    Status = e.IsActive ? "Active" : "Suspended",
                    JoinDate = e.HireDate
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        // ====== FORM: load one employee for editing ======
        // GET /api/employees/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EmployeeReadDto>> GetById(Guid id)
        {
            var e = await _db.Employees.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new EmployeeReadDto
                {
                    Id = x.Id,
                    EmployeeNumber = x.EmployeeNumber,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    Phone = x.Phone,
                    DepartmentId = x.DepartmentId,
                    PositionId = x.PositionId,   // this still holds the Position.Id
                    ManagerId = x.ManagerId,
                    HireDate = x.HireDate,
                    BirthDate = x.BirthDate,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            return e is null ? NotFound() : Ok(e);
        }

        // ====== CREATE: accepts the UI fields ======
        // POST /api/employees
        [HttpPost]
        public async Task<ActionResult<EmployeeReadDto>> Create([FromBody] EmployeeSaveDto dto)
        {
            if (await _db.Employees.AnyAsync(e => e.EmployeeNumber == dto.EmployeeNumber))
                return Conflict($"EmployeeNumber '{dto.EmployeeNumber}' already exists.");

            var (first, last) = SplitName(dto.FirstName, dto.LastName, dto.FullName);

            // 🔁 CHANGED: resolve/create Position by Title (not Name)
            Guid? positionId = dto.PositionId;
            if (!positionId.HasValue && !string.IsNullOrWhiteSpace(dto.JobTitle))
            {
                var title = dto.JobTitle.Trim();
                var pos = await _db.Positions.FirstOrDefaultAsync(p => p.Title == title);
                if (pos == null)
                {
                    pos = new Position { Title = title };
                    _db.Positions.Add(pos);
                    await _db.SaveChangesAsync();
                }
                positionId = pos.Id;
            }

            var e = new Employee
            {
                EmployeeNumber = dto.EmployeeNumber,
                FirstName = first,
                LastName = last,
                Email = dto.Email,
                Phone = dto.Phone,
                DepartmentId = dto.DepartmentId,
                PositionId = positionId,
                ManagerId = dto.ManagerId,
                HireDate = dto.HireDate,
                BirthDate = dto.BirthDate,
                Notes = dto.Notes,
                IsActive = dto.IsActive
            };

            _db.Employees.Add(e);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = e.Id }, new EmployeeReadDto
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                DepartmentId = e.DepartmentId,
                PositionId = e.PositionId,
                ManagerId = e.ManagerId,
                HireDate = e.HireDate,
                BirthDate = e.BirthDate,
                Notes = e.Notes,
                IsActive = e.IsActive
            });
        }

        // ====== UPDATE: accepts the UI fields ======
        // PUT /api/employees/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] EmployeeSaveDto dto)
        {
            var e = await _db.Employees.FindAsync(id);
            if (e is null) return NotFound();

            var (first, last) = SplitName(dto.FirstName, dto.LastName, dto.FullName);

            // 🔁 CHANGED: resolve/create Position by Title (not Name)
            Guid? positionId = dto.PositionId;
            if (!positionId.HasValue && !string.IsNullOrWhiteSpace(dto.JobTitle))
            {
                var title = dto.JobTitle.Trim();
                var pos = await _db.Positions.FirstOrDefaultAsync(p => p.Title == title);
                if (pos == null)
                {
                    pos = new Position { Title = title };
                    _db.Positions.Add(pos);
                    await _db.SaveChangesAsync();
                }
                positionId = pos.Id;
            }

            e.EmployeeNumber = dto.EmployeeNumber;
            e.FirstName = first;
            e.LastName = last;
            e.Email = dto.Email;
            e.Phone = dto.Phone;
            e.DepartmentId = dto.DepartmentId;
            e.PositionId = positionId;
            e.ManagerId = dto.ManagerId;
            e.HireDate = dto.HireDate;
            e.BirthDate = dto.BirthDate;
            e.Notes = dto.Notes;
            e.IsActive = dto.IsActive;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ====== DELETE ======
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var e = await _db.Employees.FindAsync(id);
            if (e is null) return NotFound();
            _db.Employees.Remove(e);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // helper
        private static (string first, string last) SplitName(string? first, string? last, string? full)
        {
            if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
                return (first?.Trim() ?? "", last?.Trim() ?? "");

            if (!string.IsNullOrWhiteSpace(full))
            {
                var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return (parts[0], "");
                return (parts[0], string.Join(' ', parts.Skip(1)));
            }
            return ("", "");
        }
    }
}
