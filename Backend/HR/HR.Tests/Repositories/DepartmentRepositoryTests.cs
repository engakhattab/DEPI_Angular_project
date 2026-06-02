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
}
