using HR.API;
using System.Reflection;

namespace HR.Tests.DependencyInjection;

public class Phase8BoundaryTests
{
    [Fact]
    public void Program_DelegatesRegistrationsAndAvoidsDirectServiceRegistrations()
    {
        var program = ReadRepositoryFile("HR.API", "Program.cs");

        Assert.Contains("builder.Services.AddApplication();", program);
        Assert.Contains("builder.Services.AddInfrastructure(builder.Configuration);", program);
        Assert.DoesNotContain("AddScoped<", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddTransient<", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddDbContext", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddIdentityCore", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddEntityFrameworkStores", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationProject_DoesNotReferenceInfrastructure()
    {
        var project = ReadRepositoryFile("HR.Application", "HR.Application.csproj");

        Assert.DoesNotContain("HR.Infrastructure", project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiProjectKeepsInfrastructureAsCompositionReferenceOnly()
    {
        var apiReferencesInfrastructure = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Any(a => string.Equals(a.Name, "HR.Infrastructure", StringComparison.Ordinal));

        Assert.True(apiReferencesInfrastructure);
    }

    private static string ReadRepositoryFile(params string[] relativePath)
    {
        return File.ReadAllText(GetRepositoryPath(relativePath));
    }

    private static string GetRepositoryPath(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HR.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(new[] { directory!.FullName }.Concat(relativePath).ToArray());
    }
}
