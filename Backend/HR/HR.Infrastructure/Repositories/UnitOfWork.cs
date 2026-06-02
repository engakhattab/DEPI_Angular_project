using HR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task ExecuteWithStrategyAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        await _context.Database.CreateExecutionStrategy().ExecuteAsync(
            state: operation,
            operation: static async (_, callback, token) =>
            {
                await callback(token);
                return true;
            },
            verifySucceeded: null,
            cancellationToken: ct);
    }

    public async Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new EfDataTransaction(transaction);
    }

    private sealed class EfDataTransaction(IDbContextTransaction transaction) : IDataTransaction
    {
        private readonly IDbContextTransaction _transaction = transaction;

        public Task CommitAsync(CancellationToken ct)
        {
            return _transaction.CommitAsync(ct);
        }

        public Task RollbackAsync(CancellationToken ct)
        {
            return _transaction.RollbackAsync(ct);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
