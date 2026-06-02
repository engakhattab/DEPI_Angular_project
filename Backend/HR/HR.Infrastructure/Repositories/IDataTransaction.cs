namespace HR.Infrastructure.Repositories;

public interface IDataTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
