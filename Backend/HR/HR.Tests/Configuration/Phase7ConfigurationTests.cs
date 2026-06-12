using System.Text;
using HR.Infrastructure;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HR.Tests.Configuration;

public class Phase7ConfigurationTests
{
    [Fact]
    public void BusinessSettings_WhenTimeZoneIsMissing_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["BusinessSettings:TimeZoneId"] = null
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BusinessSettings>>().Value);

        Assert.Contains(exception.Failures, f => f.Contains("BusinessSettings:TimeZoneId is required.", StringComparison.Ordinal));
    }

    [Fact]
    public void BusinessSettings_WhenTimeZoneIsInvalid_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["BusinessSettings:TimeZoneId"] = "Not/A-Timezone"
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BusinessSettings>>().Value);

        Assert.Contains(exception.Failures, f => f.Contains("BusinessSettings:TimeZoneId is invalid.", StringComparison.Ordinal));
    }

    [Fact]
    public void DocumentStorage_WhenRootPathIsMissing_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = null
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value);

        Assert.Contains(exception.Failures, f => f.Contains("DocumentStorage:RootPath is required.", StringComparison.Ordinal));
    }

    [Fact]
    public void DocumentStorage_WhenAllowedExtensionsAreMissing_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["DocumentStorage:AllowedExtensions:0"] = null
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value);

        Assert.Contains(exception.Failures, f => f.Contains("DocumentStorage:AllowedExtensions must not be empty.", StringComparison.Ordinal));
    }

    [Fact]
    public void DocumentStorage_WhenMaxFileSizeIsMissing_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["DocumentStorage:MaxFileSizeBytes"] = null
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value);

        Assert.Contains(exception.Failures, f => f.Contains("DocumentStorage:MaxFileSizeBytes must be positive.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DocumentStorage_WhenRootPathIsUnderPublicStaticFolder_FailsBeforeSaving()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "wwwroot", Guid.NewGuid().ToString("N"));
        var storage = new LocalEmployeeDocumentStorage(Options.Create(new DocumentStorageOptions
        {
            RootPath = root,
            AllowedExtensions = [".pdf"],
            MaxFileSizeBytes = 1024
        }));
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync(content, "document.pdf", "application/pdf", content.Length, CancellationToken.None));

        Assert.Contains("public static folder", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=HR.Tests;Trusted_Connection=True;",
            ["BusinessSettings:TimeZoneId"] = "Africa/Cairo",
            ["DocumentStorage:RootPath"] = "App_Data/EmployeeDocuments",
            ["DocumentStorage:AllowedExtensions:0"] = ".pdf",
            ["DocumentStorage:MaxFileSizeBytes"] = "10485760"
        };

        foreach (var (key, value) in overrides)
        {
            if (value is null)
            {
                values.Remove(key);
            }
            else
            {
                values[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
