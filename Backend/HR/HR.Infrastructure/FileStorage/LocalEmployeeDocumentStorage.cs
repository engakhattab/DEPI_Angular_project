using HR.Application.Documents;
using HR.Infrastructure.Configuration;
using HR.Shared.Results;
using Microsoft.Extensions.Options;

namespace HR.Infrastructure.FileStorage;

public class LocalEmployeeDocumentStorage(IOptions<DocumentStorageOptions> options) : IEmployeeDocumentStorage
{
    private readonly DocumentStorageOptions _options = options.Value;

    public async Task<Result<StoredEmployeeDocument>> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken ct)
    {
        if (fileSizeBytes <= 0 || content == Stream.Null)
        {
            return Result<StoredEmployeeDocument>.Failure(ServiceError.Validation("A document file is required."));
        }

        if (fileSizeBytes > _options.MaxFileSizeBytes)
        {
            return Result<StoredEmployeeDocument>.Failure(ServiceError.Validation("Uploaded document exceeds the maximum file size.", "PAYLOAD_TOO_LARGE"));
        }

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !_options.AllowedExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<StoredEmployeeDocument>.Failure(ServiceError.BusinessRule("Document file type is not allowed."));
        }

        var root = GetRootPath();
        Directory.CreateDirectory(root);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(root, storedFileName);
        var relativePath = storedFileName;

        if (!IsInsideRoot(root, fullPath))
        {
            return Result<StoredEmployeeDocument>.Failure(ServiceError.BusinessRule("Invalid document storage path."));
        }

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, ct);

        return Result<StoredEmployeeDocument>.Success(
            new StoredEmployeeDocument(storedFileName, relativePath, extension.ToLowerInvariant(), fileSizeBytes));
    }

    public Task<Stream?> OpenReadAsync(string storageRelativePath, CancellationToken ct)
    {
        var fullPath = ResolveSafePath(storageRelativePath);
        Stream? stream = File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageRelativePath, CancellationToken ct)
    {
        var fullPath = ResolveSafePath(storageRelativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveSafePath(string storageRelativePath)
    {
        if (Path.IsPathRooted(storageRelativePath) || storageRelativePath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid document storage path.");
        }

        var root = GetRootPath();
        var fullPath = Path.Combine(root, storageRelativePath);
        if (!IsInsideRoot(root, fullPath))
        {
            throw new InvalidOperationException("Invalid document storage path.");
        }

        return fullPath;
    }

    private string GetRootPath()
    {
        var configured = _options.RootPath;
        var basePath = AppContext.BaseDirectory;
        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(basePath, configured);

        var fullRoot = Path.GetFullPath(root);
        var wwwroot = Path.GetFullPath(Path.Combine(basePath, "wwwroot"));
        if (fullRoot.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DocumentStorage:RootPath must not be under a public static folder.");
        }

        return fullRoot;
    }

    private static bool IsInsideRoot(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }
}
