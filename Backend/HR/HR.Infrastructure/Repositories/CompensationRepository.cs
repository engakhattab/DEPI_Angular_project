using HR.Domain.Entities;
using HR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class CompensationRepository(ApplicationDbContext context) : ICompensationRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<EmployeeCompensation?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct)
    {
        return _context.EmployeeCompensations.FirstOrDefaultAsync(c => c.EmployeeId == employeeId, ct);
    }

    public async Task<IReadOnlyList<SalaryHistoryEntry>> GetHistoryAsync(Guid employeeId, CancellationToken ct)
    {
        var query = _context.SalaryHistoryEntries
            .AsNoTracking()
            .Where(h => h.EmployeeId == employeeId);

        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return (await query.ToListAsync(ct))
                .OrderByDescending(h => h.ChangedAt)
                .ToList();
        }

        return await query.OrderByDescending(h => h.ChangedAt).ToListAsync(ct);
    }

    public Task AddCompensationAsync(EmployeeCompensation compensation, CancellationToken ct)
    {
        return _context.EmployeeCompensations.AddAsync(compensation, ct).AsTask();
    }

    public Task AddHistoryAsync(SalaryHistoryEntry historyEntry, CancellationToken ct)
    {
        return _context.SalaryHistoryEntries.AddAsync(historyEntry, ct).AsTask();
    }
}
