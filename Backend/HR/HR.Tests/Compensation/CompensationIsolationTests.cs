using System.Reflection;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.DTOs.Employees;
using HR.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HR.Tests.Compensation;

public class CompensationIsolationTests
{
    [Theory]
    [InlineData("BaseSalary")]
    [InlineData("SalaryCurrency")]
    [InlineData("LastSalaryReviewDate")]
    [InlineData("Compensation")]
    [InlineData("SalaryHistory")]
    public void EmployeeResponse_DoesNotExposeCompensationFields(string forbiddenProperty)
    {
        var names = typeof(EmployeeResponse)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain(forbiddenProperty, names);
    }

    [Fact]
    public void SerializedEmployeeResponse_DoesNotContainCompensationFields()
    {
        var response = new EmployeeResponse
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "ISO-001",
            FullName = "Isolation Employee",
            Email = "isolation@example.com",
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            Status = EmployeeStatus.Active,
            Role = EmployeeRole.Employee,
            VacationBalanceDays = 21
        };

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("baseSalary", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salaryCurrency", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lastSalaryReviewDate", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salaryHistory", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compensation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmployeesController_DoesNotOwnCompensationEndpoints()
    {
        var compensationActions = typeof(EmployeesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(m => m.Name.Contains("Compensation", StringComparison.OrdinalIgnoreCase)
                || m.GetCustomAttributes<HttpMethodAttribute>().Any(a => a.Template?.Contains("compensation", StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

        Assert.Empty(compensationActions);
    }
}
