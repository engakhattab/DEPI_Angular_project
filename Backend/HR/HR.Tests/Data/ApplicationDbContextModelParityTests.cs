using HR.Domain.Entities;
using HR.Infrastructure.Data;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HR.Tests.Data;

public class ApplicationDbContextModelParityTests
{
    [Fact]
    public async Task Model_PreservesCorePropertyLengthsIndexesAndDeleteBehaviors()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var model = environment.Context.Model;

        var departmentEntity = model.FindEntityType(typeof(Department));
        var employeeEntity = model.FindEntityType(typeof(Employee));
        var tripEntity = model.FindEntityType(typeof(Trip));
        var vacationEntity = model.FindEntityType(typeof(VacationRequest));

        Assert.NotNull(departmentEntity);
        Assert.NotNull(employeeEntity);
        Assert.NotNull(tripEntity);
        Assert.NotNull(vacationEntity);

        Assert.Equal(100, departmentEntity!.FindProperty(nameof(Department.Name))!.GetMaxLength());
        Assert.True(departmentEntity.FindIndex(departmentEntity.FindProperty(nameof(Department.Name))!)!.IsUnique);

        Assert.Equal(20, employeeEntity!.FindProperty(nameof(Employee.EmployeeNumber))!.GetMaxLength());
        Assert.True(employeeEntity.FindIndex(employeeEntity.FindProperty(nameof(Employee.EmployeeNumber))!)!.IsUnique);
        Assert.Equal(32, employeeEntity.FindProperty(nameof(Employee.Status))!.GetMaxLength());
        Assert.Equal(21, employeeEntity.FindProperty(nameof(Employee.VacationBalanceDays))!.GetDefaultValue());
        Assert.Equal(false, employeeEntity.FindProperty(nameof(Employee.IsDeleted))!.GetDefaultValue());
        Assert.NotNull(employeeEntity.FindIndex(
            new[]
            {
                employeeEntity.FindProperty(nameof(Employee.Email))!,
                employeeEntity.FindProperty(nameof(Employee.IsDeleted))!,
                employeeEntity.FindProperty(nameof(Employee.Status))!
            }));
        Assert.Equal(
            DeleteBehavior.Restrict,
            employeeEntity.FindNavigation(nameof(Employee.Department))!.ForeignKey.DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            employeeEntity.FindNavigation(nameof(Employee.Manager))!.ForeignKey.DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Cascade,
            employeeEntity.GetForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(Employee.ApplicationUserId)).DeleteBehavior);

        Assert.Equal(32, tripEntity!.FindProperty(nameof(Trip.TripCode))!.GetMaxLength());
        Assert.True(tripEntity.FindIndex(tripEntity.FindProperty(nameof(Trip.TripCode))!)!.IsUnique);
        Assert.True(tripEntity.FindIndex(tripEntity.FindProperty(nameof(Trip.RequestCode))!)!.IsUnique);
        Assert.NotNull(tripEntity.FindIndex(tripEntity.FindProperty(nameof(Trip.RequestedByEmployeeId))!));
        Assert.Equal(
            DeleteBehavior.Restrict,
            tripEntity.FindNavigation(nameof(Trip.RequestedBy))!.ForeignKey.DeleteBehavior);

        Assert.Equal(500, vacationEntity!.FindProperty(nameof(VacationRequest.Reason))!.GetMaxLength());
        Assert.NotNull(vacationEntity.FindIndex(vacationEntity.FindProperty(nameof(VacationRequest.ReviewedByEmployeeId))!));
        Assert.NotNull(vacationEntity.FindIndex(
            new[]
            {
                vacationEntity.FindProperty(nameof(VacationRequest.EmployeeId))!,
                vacationEntity.FindProperty(nameof(VacationRequest.Status))!,
                vacationEntity.FindProperty(nameof(VacationRequest.StartDate))!,
                vacationEntity.FindProperty(nameof(VacationRequest.EndDate))!
            }));
        Assert.Equal(
            DeleteBehavior.Cascade,
            vacationEntity.FindNavigation(nameof(VacationRequest.Employee))!.ForeignKey.DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            vacationEntity.FindNavigation(nameof(VacationRequest.ReviewedBy))!.ForeignKey.DeleteBehavior);
    }
}
