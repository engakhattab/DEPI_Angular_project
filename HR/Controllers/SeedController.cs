using HR.Data;
using HR.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly HrDbContext _db;
        public SeedController(HrDbContext db) => _db = db;

        [HttpPost("sample")]
        public async Task<IActionResult> CreateSampleData()
        {
            // 1) Ensure LeaveTypes (in case migrations didn't seed yet)
            if (!await _db.LeaveTypes.AnyAsync())
            {
                _db.LeaveTypes.AddRange(
                    new LeaveType { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Annual Leave", Code = "ANL", IsPaid = true, MaxPerYearDays = 21 },
                    new LeaveType { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Sick Leave", Code = "SICK", IsPaid = true, MaxPerYearDays = 10 }
                );
                await _db.SaveChangesAsync();
            }
            var annual = await _db.LeaveTypes.SingleAsync(x => x.Code == "ANL");
            var sick = await _db.LeaveTypes.SingleAsync(x => x.Code == "SICK");

            // 2) Departments
            if (!await _db.Departments.AnyAsync())
            {
                _db.Departments.AddRange(
                    new Department { Name = "Human Resources", Code = "HR" },
                    new Department { Name = "Information Technology", Code = "IT" },
                    new Department { Name = "Sales", Code = "SALES" }
                );
                await _db.SaveChangesAsync();
            }
            var hr = await _db.Departments.SingleAsync(d => d.Code == "HR");
            var it = await _db.Departments.SingleAsync(d => d.Code == "IT");
            var sales = await _db.Departments.SingleAsync(d => d.Code == "SALES");

            // 3) Positions
            if (!await _db.Positions.AnyAsync())
            {
                _db.Positions.AddRange(
                    new Position { DepartmentId = hr.Id, Title = "HR Manager", Code = "HRM", IsManagerial = true },
                    new Position { DepartmentId = hr.Id, Title = "HR Specialist", Code = "HRS" },

                    new Position { DepartmentId = it.Id, Title = "IT Manager", Code = "ITM", IsManagerial = true },
                    new Position { DepartmentId = it.Id, Title = "Software Engineer", Code = "SE" },
                    new Position { DepartmentId = it.Id, Title = "QA Engineer", Code = "QA" },

                    new Position { DepartmentId = sales.Id, Title = "Sales Manager", Code = "SM", IsManagerial = true },
                    new Position { DepartmentId = sales.Id, Title = "Sales Rep", Code = "SR" }
                );
                await _db.SaveChangesAsync();
            }
            var posHrManager = await _db.Positions.SingleAsync(p => p.Code == "HRM");
            var posHrSpec = await _db.Positions.SingleAsync(p => p.Code == "HRS");
            var posItMgr = await _db.Positions.SingleAsync(p => p.Code == "ITM");
            var posSe = await _db.Positions.SingleAsync(p => p.Code == "SE");
            var posQa = await _db.Positions.SingleAsync(p => p.Code == "QA");
            var posSm = await _db.Positions.SingleAsync(p => p.Code == "SM");
            var posSr = await _db.Positions.SingleAsync(p => p.Code == "SR");

            // 4) Employees (create managers first so we can reference them)
            if (!await _db.Employees.AnyAsync())
            {
                var hrManager = new Employee
                {
                    EmployeeNumber = "E1001",
                    FirstName = "Sara",
                    LastName = "Mahmoud",
                    Email = "sara@company.local",
                    HireDate = DateTime.UtcNow.Date.AddYears(-3),
                    DepartmentId = hr.Id,
                    PositionId = posHrManager.Id
                };

                var itManager = new Employee
                {
                    EmployeeNumber = "E1002",
                    FirstName = "Omar",
                    LastName = "Hassan",
                    Email = "omar@company.local",
                    HireDate = DateTime.UtcNow.Date.AddYears(-2),
                    DepartmentId = it.Id,
                    PositionId = posItMgr.Id
                };

                _db.Employees.AddRange(hrManager, itManager);
                await _db.SaveChangesAsync();

                var se1 = new Employee
                {
                    EmployeeNumber = "E1003",
                    FirstName = "Rana",
                    LastName = "Ali",
                    Email = "rana@company.local",
                    HireDate = DateTime.UtcNow.Date.AddYears(-1),
                    DepartmentId = it.Id,
                    PositionId = posSe.Id,
                    ManagerId = itManager.Id
                };

                var salesRep = new Employee
                {
                    EmployeeNumber = "E1004",
                    FirstName = "Khaled",
                    LastName = "Saad",
                    Email = "khaled@company.local",
                    HireDate = DateTime.UtcNow.Date.AddMonths(-9),
                    DepartmentId = sales.Id,
                    PositionId = posSr.Id
                };

                _db.Employees.AddRange(se1, salesRep);
                await _db.SaveChangesAsync();

                // 5) A sample Leave Request for Rana (Annual Leave 2.0 days next week)
                _db.LeaveRequests.Add(new LeaveRequest
                {
                    EmployeeId = se1.Id,
                    LeaveTypeId = annual.Id,
                    StartDate = DateTime.UtcNow.Date.AddDays(7),
                    EndDate = DateTime.UtcNow.Date.AddDays(8),
                    Days = 2.0m,
                    Reason = "Family trip"
                });

                // 6) A sample attendance record for yesterday for Rana
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EmployeeId = se1.Id,
                    WorkDate = yesterday,
                    CheckIn = yesterday.AddHours(8).AddMinutes(55),
                    CheckOut = yesterday.AddHours(17).AddMinutes(5),
                    Status = "Present",
                    OvertimeMinutes = 10
                });

                await _db.SaveChangesAsync();
            }

            // Return a quick summary with IDs you can use from Swagger
            var result = new
            {
                Departments = await _db.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new { d.Id, d.Name, d.Code }).ToListAsync(),
                Positions = await _db.Positions
                    .OrderBy(p => p.Title)
                    .Select(p => new { p.Id, p.Title, p.Code, p.DepartmentId }).ToListAsync(),
                Employees = await _db.Employees
                    .OrderBy(e => e.EmployeeNumber)
                    .Select(e => new { e.Id, e.EmployeeNumber, Name = e.FirstName + " " + e.LastName, e.DepartmentId, e.PositionId, e.ManagerId })
                    .ToListAsync(),
                LeaveTypes = await _db.LeaveTypes
                    .OrderBy(t => t.Code)
                    .Select(t => new { t.Id, t.Name, t.Code, t.MaxPerYearDays })
                    .ToListAsync()
            };

            return Ok(result);
        }

    }
}
