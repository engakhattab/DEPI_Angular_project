using System.Reflection;
using HR.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace HR.Tests.Authorization;

public class BootstrapStartupValidationTests
{
    [Fact]
    public void Program_InvokesBootstrapBeforeAuthorizationMiddleware()
    {
        var programText = File.ReadAllText(FindWorkspaceFile("HR.API", "Program.cs"));
        var bootstrapIndex = programText.IndexOf("InitialSystemAdminBootstrapper", StringComparison.Ordinal);
        var authorizationIndex = programText.IndexOf("app.UseAuthorization()", StringComparison.Ordinal);

        Assert.True(bootstrapIndex >= 0);
        Assert.True(authorizationIndex >= 0);
        Assert.True(bootstrapIndex < authorizationIndex);
    }

    [Fact]
    public void CreateEmployeeEndpoint_RemainsProtectedAndIsNotPublicBootstrapPath()
    {
        var controllerAuthorize = typeof(EmployeesController).GetCustomAttribute<AuthorizeAttribute>();
        var createMethod = typeof(EmployeesController).GetMethod(nameof(EmployeesController.CreateEmployee));
        var allowAnonymous = createMethod!.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Null(allowAnonymous);
    }

    private static string FindWorkspaceFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find workspace file '{Path.Combine(relativeParts)}'.");
    }
}
