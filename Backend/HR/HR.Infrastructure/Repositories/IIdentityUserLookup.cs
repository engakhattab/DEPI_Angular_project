using HR.Infrastructure.Identity;

namespace HR.Infrastructure.Repositories;

public interface IIdentityUserLookup
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyDictionary<string, ApplicationUser>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct);
}
