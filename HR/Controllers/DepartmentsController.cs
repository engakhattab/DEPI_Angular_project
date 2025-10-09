using HR.Data;
using HR.DTOs.Departments;
using HR.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly HrDbContext _db;
        public DepartmentsController(HrDbContext db) => _db = db;

        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<Department>>> GetTree()
        {
            var all = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return Ok(all);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> Get()
        {
            var list = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<Department>> Create([FromBody] DepartmentCreateDto dto)
        {
            if (await _db.Departments.AnyAsync(d => d.Code == dto.Code))
                return Conflict($"Department code '{dto.Code}' already exists.");

            var dep = new Department
            {
                Name = dto.Name,
                Code = dto.Code,
                ParentDepartmentId = dto.ParentDepartmentId
            };

            _db.Departments.Add(dep);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = dep.Id }, dep);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] DepartmentCreateDto dto)
        {
            var dep = await _db.Departments.FindAsync(id);
            if (dep is null) return NotFound();
            dep.Name = dto.Name;
            dep.Code = dto.Code;
            dep.ParentDepartmentId = dto.ParentDepartmentId;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var dep = await _db.Departments.FindAsync(id);
            if (dep is null) return NotFound();
            _db.Departments.Remove(dep);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
