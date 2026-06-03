using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Infrastructure.Data.Pagination;
using HR.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class DepartmentRepository(ApplicationDbContext context) : IDepartmentRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<PagedList<Department>> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name);

        return PagedQueryExecutor.ExecuteAsync(query, page, pageSize, ct);
    }

    public Task<PagedList<Department>> GetPageWithEmployeeCountsAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees.Where(employee => !employee.IsDeleted))
            .OrderBy(d => d.Name);

        return PagedQueryExecutor.ExecuteAsync(query, page, pageSize, ct);
    }

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public Task<Department?> GetByIdWithEmployeeCountAsync(Guid id, CancellationToken ct)
    {
        return _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees.Where(employee => !employee.IsDeleted))
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public Task<Department?> GetByIdWithEmployeesAsync(Guid id, CancellationToken ct)
    {
        return _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken ct)
    {
        return _context.Departments.AnyAsync(
            d => d.Name == name && (!excludingId.HasValue || d.Id != excludingId.Value),
            ct);
    }

    public Task AddAsync(Department department, CancellationToken ct)
    {
        return _context.Departments.AddAsync(department, ct).AsTask();
    }

    public void Remove(Department department)
    {
        _context.Departments.Remove(department);
    }
}
