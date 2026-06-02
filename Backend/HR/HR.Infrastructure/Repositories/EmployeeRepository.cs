using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class EmployeeRepository(ApplicationDbContext context) : IEmployeeRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<PagedList<Employee>> GetPageWithDetailsAsync(
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .OrderBy(e => e.EmployeeNumber);

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        return PagedQueryExecutor.ExecuteAsync(query, page, pageSize, ct);
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<Employee?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<Employee?> GetByApplicationUserIdWithDetailsAsync(string applicationUserId, CancellationToken ct)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId, ct);
    }

    public Task<Employee?> GetByEmployeeNumberWithDetailsAsync(string employeeNumber, CancellationToken ct)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber, ct);
    }

    public async Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct)
    {
        return await _context.Employees
            .Where(e => e.ManagerId == managerId)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return _context.Employees.AnyAsync(e => e.Id == id, ct);
    }

    public Task<bool> ExistsByNumberAsync(string employeeNumber, CancellationToken ct)
    {
        return _context.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber, ct);
    }

    public Task AddAsync(Employee employee, CancellationToken ct)
    {
        return _context.Employees.AddAsync(employee, ct).AsTask();
    }

    public void Remove(Employee employee)
    {
        _context.Employees.Remove(employee);
    }
}
