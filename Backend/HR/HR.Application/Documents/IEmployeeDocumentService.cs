using HR.Application.DTOs.Documents;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Application.Documents;

public interface IEmployeeDocumentService
{
    Task<Result<EmployeeDocumentResponse>> UploadAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentUploadRequest request, CancellationToken ct);
    Task<Result<PagedList<EmployeeDocumentResponse>>> ListAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentQueryRequest request, CancellationToken ct);
    Task<Result<EmployeeDocumentDownload>> DownloadAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct);
    Task<Result> RemoveAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct);
}

public sealed record EmployeeDocumentDownload(Stream Content, string FileName, string ContentType);
