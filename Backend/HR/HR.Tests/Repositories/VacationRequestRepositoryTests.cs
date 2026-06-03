using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Repositories;

public class VacationRequestRepositoryTests
{
    [Fact]
    public async Task GetPageWithEmployeeAsync_AppliesFiltersAndNewestFirstOrdering()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employeeA = await environment.AddEmployeeAsync("EMP-201", "employee-a@example.com", environment.DefaultDepartment!.Id);
        var employeeB = await environment.AddEmployeeAsync("EMP-202", "employee-b@example.com", environment.DefaultDepartment!.Id);
        var older = await environment.AddVacationRequestAsync(employeeA.Id, VacationRequestStatus.Approved, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var newer = await environment.AddVacationRequestAsync(employeeA.Id, VacationRequestStatus.Approved, new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero));
        await environment.AddVacationRequestAsync(employeeB.Id, VacationRequestStatus.Pending, new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));

        var repository = new VacationRequestRepository(environment.Context);

        var filteredPage = await repository.GetPageWithEmployeeAsync(
            VacationRequestStatus.Approved,
            employeeA.Id,
            1,
            25,
            CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], filteredPage.Items.Select(v => v.Id).ToArray());
        Assert.All(filteredPage.Items, item => Assert.Equal(employeeA.Id, item.EmployeeId));
        Assert.All(filteredPage.Items, item => Assert.Equal(VacationRequestStatus.Approved, item.Status));
    }

    [Fact]
    public async Task LookupMethods_ReturnEmployeeDetailAndEmployeeScopedResults()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("EMP-203", "employee-c@example.com", environment.DefaultDepartment!.Id);
        var request = await environment.AddVacationRequestAsync(employee.Id, VacationRequestStatus.Pending, DateTimeOffset.UtcNow);
        var repository = new VacationRequestRepository(environment.Context);

        var withEmployee = await repository.GetByIdWithEmployeeAsync(request.Id, CancellationToken.None);
        var byEmployee = await repository.GetByEmployeeIdAsync(employee.Id, CancellationToken.None);

        Assert.NotNull(withEmployee);
        Assert.Equal(employee.FullName, withEmployee!.Employee?.FullName);
        Assert.Single(byEmployee);
        Assert.Equal(request.Id, byEmployee.Single().Id);
    }

    [Fact]
    public async Task HasOverlappingPendingOrApprovedAsync_UsesInclusiveBoundariesAndIgnoresRejectedRows()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var employee = await environment.AddEmployeeAsync("EMP-204", "employee-d@example.com", environment.DefaultDepartment!.Id);
        await environment.AddVacationRequestAsync(
            employee.Id,
            VacationRequestStatus.Pending,
            DateTimeOffset.UtcNow,
            startDate: new DateOnly(2026, 6, 14),
            endDate: new DateOnly(2026, 6, 16));
        await environment.AddVacationRequestAsync(
            employee.Id,
            VacationRequestStatus.Rejected,
            DateTimeOffset.UtcNow,
            startDate: new DateOnly(2026, 6, 20),
            endDate: new DateOnly(2026, 6, 21));

        var repository = new VacationRequestRepository(environment.Context);

        var boundaryOverlap = await repository.HasOverlappingPendingOrApprovedAsync(
            employee.Id,
            new DateOnly(2026, 6, 16),
            new DateOnly(2026, 6, 18),
            CancellationToken.None);
        var rejectedOnlyOverlap = await repository.HasOverlappingPendingOrApprovedAsync(
            employee.Id,
            new DateOnly(2026, 6, 20),
            new DateOnly(2026, 6, 21),
            CancellationToken.None);

        Assert.True(boundaryOverlap);
        Assert.False(rejectedOnlyOverlap);
    }
}
