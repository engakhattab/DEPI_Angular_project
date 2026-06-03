using HR.Application.Departments;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Departments;

public class DepartmentServiceBusinessRuleTests
{
    [Fact]
    public async Task DepartmentCounts_ExcludeSoftDeletedAndRetainTerminatedEmployees()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var service = environment.GetRequiredService<IDepartmentService>();
        var engineering = environment.DefaultDepartment!;
        var support = await environment.AddDepartmentAsync("Support");

        var engineeringActive = await environment.AddEmployeeAsync("EMP-1101", "eng1101@example.com", engineering.Id);
        var supportActive = await environment.AddEmployeeAsync("EMP-1102", "sup1102@example.com", support.Id);
        await environment.AddEmployeeAsync(
            "EMP-1103",
            "eng1103@example.com",
            engineering.Id,
            status: EmployeeStatus.Terminated,
            terminatedAt: new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        await environment.AddEmployeeAsync(
            "EMP-1104",
            "eng1104@example.com",
            engineering.Id,
            status: EmployeeStatus.Terminated,
            isDeleted: true,
            terminatedAt: new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));

        var initialPage = await service.GetDepartmentsAsync(1, 25, CancellationToken.None);
        Assert.Equal(2, initialPage.Items.Single(d => d.Id == engineering.Id).EmployeeCount);
        Assert.Equal(1, initialPage.Items.Single(d => d.Id == support.Id).EmployeeCount);

        supportActive.DepartmentId = engineering.Id;
        await environment.Context.SaveChangesAsync();

        var afterMove = await service.GetDepartmentByIdAsync(engineering.Id, CancellationToken.None);
        Assert.NotNull(afterMove);
        Assert.Equal(3, afterMove!.EmployeeCount);

        engineeringActive.Status = EmployeeStatus.Terminated;
        engineeringActive.TerminatedAt = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        await environment.Context.SaveChangesAsync();

        var afterTermination = await service.GetDepartmentByIdAsync(engineering.Id, CancellationToken.None);
        Assert.NotNull(afterTermination);
        Assert.Equal(3, afterTermination!.EmployeeCount);

        supportActive.IsDeleted = true;
        supportActive.Status = EmployeeStatus.Terminated;
        supportActive.TerminatedAt = new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);
        await environment.Context.SaveChangesAsync();

        var afterSoftDelete = await service.GetDepartmentByIdAsync(engineering.Id, CancellationToken.None);
        Assert.NotNull(afterSoftDelete);
        Assert.Equal(2, afterSoftDelete!.EmployeeCount);
    }
}
