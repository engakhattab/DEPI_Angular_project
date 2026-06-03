using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Repositories;

public class DepartmentRepositoryTests
{
    [Fact]
    public async Task GetPageAsync_ReturnsAlphabeticalPageAndLookupData()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var alpha = await environment.AddDepartmentAsync("Alpha");
        var bravo = await environment.AddDepartmentAsync("Bravo");
        await environment.AddDepartmentAsync("Charlie");
        await environment.AddEmployeeAsync("EMP-100", "alpha@example.com", alpha.Id);

        var repository = new DepartmentRepository(environment.Context);

        var page = await repository.GetPageAsync(1, 2, CancellationToken.None);
        var withEmployees = await repository.GetByIdWithEmployeesAsync(alpha.Id, CancellationToken.None);

        Assert.Equal(["Alpha", "Bravo"], page.Items.Select(d => d.Name).ToArray());
        Assert.NotNull(withEmployees);
        Assert.Single(withEmployees!.Employees);
        Assert.Equal("EMP-100", withEmployees.Employees.Single().EmployeeNumber);
    }

    [Fact]
    public async Task ExistsByNameAsync_HonorsExcludingId()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var department = await environment.AddDepartmentAsync("Finance");
        var repository = new DepartmentRepository(environment.Context);

        Assert.True(await repository.ExistsByNameAsync("Finance", null, CancellationToken.None));
        Assert.False(await repository.ExistsByNameAsync("Finance", department.Id, CancellationToken.None));
    }

    [Fact]
    public async Task EmployeeCountQueries_ExcludeSoftDeletedAndRetainTerminatedEmployees()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var alpha = await environment.AddDepartmentAsync("Alpha");
        var bravo = await environment.AddDepartmentAsync("Bravo");
        await environment.AddEmployeeAsync("EMP-1201", "alpha1201@example.com", alpha.Id);
        await environment.AddEmployeeAsync(
            "EMP-1202",
            "alpha1202@example.com",
            alpha.Id,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            terminatedAt: DateTimeOffset.UtcNow);
        await environment.AddEmployeeAsync(
            "EMP-1203",
            "alpha1203@example.com",
            alpha.Id,
            status: HR.Domain.Enums.EmployeeStatus.Terminated,
            isDeleted: true,
            terminatedAt: DateTimeOffset.UtcNow);
        await environment.AddEmployeeAsync("EMP-1204", "bravo1204@example.com", bravo.Id);
        var repository = new DepartmentRepository(environment.Context);

        var page = await repository.GetPageWithEmployeeCountsAsync(1, 25, CancellationToken.None);
        var alphaDetail = await repository.GetByIdWithEmployeeCountAsync(alpha.Id, CancellationToken.None);

        Assert.Equal(2, page.Items.Single(d => d.Id == alpha.Id).Employees.Count);
        Assert.Single(page.Items.Single(d => d.Id == bravo.Id).Employees);
        Assert.NotNull(alphaDetail);
        Assert.Equal(2, alphaDetail!.Employees.Count);
    }
}
