using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Repositories;

public class EmployeeRepositoryTests
{
    [Fact]
    public async Task GetPageWithDetailsAsync_AppliesStatusFilterOrderingAndLoadsDetails()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var department = await environment.AddDepartmentAsync("Operations");
        var manager = await environment.AddEmployeeAsync("EMP-300", "manager@example.com", department.Id);
        var active = await environment.AddEmployeeAsync("EMP-301", "active@example.com", department.Id, manager.Id, EmployeeStatus.Active);
        await environment.AddEmployeeAsync("EMP-302", "terminated@example.com", department.Id, null, EmployeeStatus.Terminated);
        var repository = new EmployeeRepository(environment.Context);

        var page = await repository.GetPageWithDetailsAsync(EmployeeStatus.Active, 1, 25, CancellationToken.None);
        var employee = page.Items.Single(e => e.Id == active.Id);

        Assert.Equal(["EMP-300", "EMP-301"], page.Items.Select(e => e.EmployeeNumber).ToArray());
        Assert.Equal("Operations", employee.Department?.Name);
        Assert.Equal(manager.Id, employee.ManagerId);
        Assert.Equal(manager.FullName, employee.Manager?.FullName);
    }

    [Fact]
    public async Task LookupMethods_ReturnProfileDetailDirectReportsAndExistenceChecks()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var department = await environment.AddDepartmentAsync("Technology");
        var manager = await environment.AddEmployeeAsync("EMP-310", "manager2@example.com", department.Id);
        var report = await environment.AddEmployeeAsync("EMP-311", "report2@example.com", department.Id, manager.Id);
        var repository = new EmployeeRepository(environment.Context);

        var byId = await repository.GetByIdWithDetailsAsync(report.Id, CancellationToken.None);
        var byUserId = await repository.GetByApplicationUserIdWithDetailsAsync(report.ApplicationUserId, CancellationToken.None);
        var byNumber = await repository.GetByEmployeeNumberWithDetailsAsync(report.EmployeeNumber, CancellationToken.None);
        var directReports = await repository.GetDirectReportsAsync(manager.Id, CancellationToken.None);

        Assert.NotNull(byId);
        Assert.NotNull(byUserId);
        Assert.NotNull(byNumber);
        Assert.Equal(report.Id, byUserId!.Id);
        Assert.Equal(report.Id, byNumber!.Id);
        Assert.Single(directReports);
        Assert.Equal(report.Id, directReports.Single().Id);
        Assert.True(await repository.ExistsAsync(report.Id, CancellationToken.None));
        Assert.True(await repository.ExistsByNumberAsync(report.EmployeeNumber, CancellationToken.None));
    }
}
