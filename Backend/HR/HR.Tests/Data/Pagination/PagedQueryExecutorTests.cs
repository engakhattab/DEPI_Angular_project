using HR.Domain.Entities;
using HR.Infrastructure.Data.Pagination;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Data.Pagination;

public class PagedQueryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_NormalizesPagingAndReturnsExpectedSlice()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();

        for (var i = 0; i < 120; i++)
        {
            environment.Context.Departments.Add(new Department { Name = $"Dept-{i:D3}" });
        }

        await environment.Context.SaveChangesAsync();

        var page = await PagedQueryExecutor.ExecuteAsync(
            environment.Context.Departments.AsNoTracking().OrderBy(d => d.Name),
            0,
            150,
            CancellationToken.None);

        Assert.Equal(1, page.Page);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(120, page.TotalCount);
        Assert.Equal(100, page.Items.Count);
        Assert.Equal("Dept-000", page.Items[0].Name);
        Assert.Equal("Dept-099", page.Items[^1].Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        environment.Context.Departments.Add(new Department { Name = "Dept-001" });
        await environment.Context.SaveChangesAsync();

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PagedQueryExecutor.ExecuteAsync(
                environment.Context.Departments.AsNoTracking().OrderBy(d => d.Name),
                1,
                25,
                cancellationSource.Token));
    }
}
