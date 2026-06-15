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
            .Where(e => !e.IsDeleted)
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
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);
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

    public async Task<IReadOnlyList<Employee>> FindByEmailOrEmployeeNumberAsync(string identifier, CancellationToken ct)
    {
        var normalized = identifier.Trim().ToUpperInvariant();
        return await _context.Employees
            .Where(e => e.EmployeeNumber.ToUpper() == normalized
                || (e.Email != null && e.Email.ToUpper() == normalized))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct)
    {
        return await _context.Employees
            .Where(e => e.ManagerId == managerId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetAllActiveAsync(CancellationToken ct)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlySet<Guid>> GetDirectAndIndirectReportIdsAsync(Guid managerId, CancellationToken ct)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync(ct);

        var result = new HashSet<Guid>();
        var frontier = employees.Where(e => e.ManagerId == managerId).Select(e => e.Id).ToList();

        while (frontier.Count > 0)
        {
            var current = frontier[^1];
            frontier.RemoveAt(frontier.Count - 1);
            if (!result.Add(current))
            {
                continue;
            }

            frontier.AddRange(employees.Where(e => e.ManagerId == current).Select(e => e.Id));
        }

        return result;
    }

    public Task<PagedList<Employee>> GetScopedPageAsync(
        IReadOnlySet<Guid> allowedIds,
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Where(e => allowedIds.Contains(e.Id))
            .OrderBy(e => e.EmployeeNumber);

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        return PagedQueryExecutor.ExecuteAsync(query, page, pageSize, ct);
    }

    public Task<PagedList<Employee>> GetOrganizationWidePageAsync(
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

    public Task<Employee?> GetByIdWithDetailsIncludingSoftDeletedAsync(Guid id, CancellationToken ct)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<bool> AnyActiveSystemAdministratorAsync(CancellationToken ct)
    {
        return _context.Employees.AnyAsync(
            e => !e.IsDeleted
                && e.Status == EmployeeStatus.Active
                && e.Role == EmployeeRole.SystemAdministrator,
            ct);
    }

    public Task<int> GetActiveSystemAdministratorCountAsync(CancellationToken ct)
    {
        return _context.Employees.CountAsync(
            e => !e.IsDeleted
                && e.Status == EmployeeStatus.Active
                && e.Role == EmployeeRole.SystemAdministrator,
            ct);
    }

    public Task<bool> ExistsIncludingSoftDeletedAsync(Guid id, CancellationToken ct)
    {
        return _context.Employees.AnyAsync(e => e.Id == id, ct);
    }

    public Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return _context.Employees.AnyAsync(
            e => e.Email != null && e.Email.ToUpper() == normalizedEmail,
            ct);
    }

    public Task<bool> ExistsActiveWithEmailAsync(string email, Guid? excludingEmployeeId, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return _context.Employees.AnyAsync(
            e => e.Email != null
                && e.Email.ToUpper() == normalizedEmail
                && e.Status == EmployeeStatus.Active
                && !e.IsDeleted
                && (!excludingEmployeeId.HasValue || e.Id != excludingEmployeeId.Value),
            ct);
    }

    public Task<Guid?> GetManagerIdAsync(Guid employeeId, CancellationToken ct)
    {
        return _context.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.ManagerId)
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> IsAuthenticationEligibleAsync(Guid employeeId, CancellationToken ct)
    {
        return _context.Employees.AnyAsync(
            e => e.Id == employeeId
                && !e.IsDeleted
                && e.Status != EmployeeStatus.Terminated,
            ct);
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
