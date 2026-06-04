namespace HR.Tests.DependencyInjection;

public class RegistrationBoundaryTests
{
    [Fact]
    public void Program_DelegatesApplicationAndInfrastructureRegistration()
    {
        var program = ReadRepositoryFile("HR.API", "Program.cs");

        Assert.Contains("builder.Services.AddApplication();", program);
        Assert.Contains("builder.Services.AddInfrastructure(builder.Configuration);", program);
        Assert.DoesNotContain("AddScoped<", program);
        Assert.DoesNotContain("AddDbContext", program);
        Assert.DoesNotContain("AddIdentityCore", program);
        Assert.DoesNotContain("AddEntityFrameworkStores", program);
    }

    [Fact]
    public void ApplicationProject_DoesNotReferenceInfrastructureOrPersistenceImplementations()
    {
        var project = ReadRepositoryFile("HR.Application", "HR.Application.csproj");

        Assert.DoesNotContain("HR.Infrastructure", project);
        Assert.DoesNotContain("EntityFrameworkCore.SqlServer", project);
        Assert.DoesNotContain("Identity.EntityFrameworkCore", project);
    }

    [Fact]
    public void InfrastructureProject_OwnsPersistenceAndIdentityPackages()
    {
        var project = ReadRepositoryFile("HR.Infrastructure", "HR.Infrastructure.csproj");

        Assert.Contains("Microsoft.EntityFrameworkCore.SqlServer", project);
        Assert.Contains("Microsoft.AspNetCore.Identity.EntityFrameworkCore", project);
    }

    [Fact]
    public void ServiceClasses_DoNotReferenceApplicationDbContextDirectly()
    {
        var infrastructureRoot = GetRepositoryPath("HR.Infrastructure");
        var serviceFiles = Directory.GetFiles(infrastructureRoot, "*Service.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(serviceFiles);
        foreach (var file in serviceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("ApplicationDbContext", content);
        }
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
