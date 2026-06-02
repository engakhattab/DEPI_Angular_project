using HR.Infrastructure.Data;
using HR.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Repositories;

public class IdentityUserLookup(ApplicationDbContext context) : IIdentityUserLookup
{
    private readonly ApplicationDbContext _context = context;

    public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IReadOnlyDictionary<string, ApplicationUser>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<string, ApplicationUser>();
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Id);
    }
}
