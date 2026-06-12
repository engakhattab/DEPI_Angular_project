using System.Text;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.FileStorage;
using Microsoft.Extensions.Options;

namespace HR.Tests.Documents;

public class LocalEmployeeDocumentStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hr-doc-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_GeneratesSafeStoredNameAndDoesNotUseOriginalFileName()
    {
        var storage = CreateStorage(_root);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document"));

        var result = await storage.SaveAsync(content, @"..\original Contract.PDF", "application/pdf", content.Length, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.EndsWith(".pdf", result.Value!.StoredFileName);
        Assert.DoesNotContain("original", result.Value.StoredFileName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("..", result.Value.StoredFileName, StringComparison.Ordinal);
        Assert.Equal(result.Value.StoredFileName, result.Value.StorageRelativePath);
        Assert.True(File.Exists(Path.Combine(_root, result.Value.StorageRelativePath)));
    }

    [Fact]
    public async Task SaveAsync_WhenExtensionIsNotAllowed_ReturnsBusinessRule()
    {
        var storage = CreateStorage(_root);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document"));

        var result = await storage.SaveAsync(content, "malware.exe", "application/octet-stream", content.Length, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public async Task SaveAsync_WhenFileIsOversized_ReturnsPayloadTooLarge()
    {
        var storage = CreateStorage(_root, maxFileSizeBytes: 4);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document"));

        var result = await storage.SaveAsync(content, "document.pdf", "application/pdf", content.Length, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PAYLOAD_TOO_LARGE", result.Error!.Code);
    }

    [Fact]
    public async Task SaveAsync_WhenRootIsUnderPublicStaticFolder_ThrowsConfigurationError()
    {
        var publicRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", Guid.NewGuid().ToString("N"));
        var storage = CreateStorage(publicRoot);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync(content, "document.pdf", "application/pdf", content.Length, CancellationToken.None));

        Assert.Contains("public static folder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../secret.pdf")]
    [InlineData("folder/../../secret.pdf")]
    public async Task OpenReadAsync_WhenRelativePathAttemptsTraversal_Throws(string relativePath)
    {
        var storage = CreateStorage(_root);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.OpenReadAsync(relativePath, CancellationToken.None));

        Assert.Contains("Invalid document storage path", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenPathIsAbsolute_Throws()
    {
        var storage = CreateStorage(_root);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.DeleteAsync(Path.Combine(_root, "document.pdf"), CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static LocalEmployeeDocumentStorage CreateStorage(string rootPath, long maxFileSizeBytes = 1024)
    {
        return new LocalEmployeeDocumentStorage(Options.Create(new DocumentStorageOptions
        {
            RootPath = rootPath,
            AllowedExtensions = [".pdf", ".png"],
            MaxFileSizeBytes = maxFileSizeBytes
        }));
    }
}
