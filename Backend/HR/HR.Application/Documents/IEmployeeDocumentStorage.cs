using HR.Shared.Results;

namespace HR.Application.Documents;

public interface IEmployeeDocumentStorage
{
    Task<Result<StoredEmployeeDocument>> SaveAsync(Stream content, string originalFileName, string contentType, long fileSizeBytes, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storageRelativePath, CancellationToken ct);
    Task DeleteAsync(string storageRelativePath, CancellationToken ct);
}

public sealed record StoredEmployeeDocument(
    string StoredFileName,
    string StorageRelativePath,
    string FileExtension,
    long FileSizeBytes);
