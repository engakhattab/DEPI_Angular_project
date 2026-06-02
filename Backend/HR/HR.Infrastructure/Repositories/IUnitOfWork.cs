namespace HR.Infrastructure.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);

    Task ExecuteWithStrategyAsync(Func<CancellationToken, Task> operation, CancellationToken ct);

    Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct);
}
