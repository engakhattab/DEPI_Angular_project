using HR.Application.Authorization;
using HR.Application.Documents;
using HR.Application.DTOs.Documents;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Documents;

public class EmployeeDocumentService(
    IEmployeeDocumentRepository documentRepository,
    IEmployeeDocumentStorage storage,
    IEmployeeRepository employeeRepository,
    IEmployeeAccessService accessService,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IEmployeeDocumentService
{
    private readonly IEmployeeDocumentRepository _documentRepository = documentRepository;
    private readonly IEmployeeDocumentStorage _storage = storage;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IEmployeeAccessService _accessService = accessService;
    private readonly IAuditWriter _auditWriter = auditWriter;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<EmployeeDocumentResponse>> UploadAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentUploadRequest request, CancellationToken ct)
    {
        var auth = await AuthorizeAsync(requesterEmployeeId, employeeId, ct);
        if (auth is not null)
        {
            return Result<EmployeeDocumentResponse>.Failure(auth);
        }

        var stored = await _storage.SaveAsync(request.Content, request.OriginalFileName, request.ContentType, request.FileSizeBytes, ct);
        if (stored.IsFailure)
        {
            return Result<EmployeeDocumentResponse>.Failure(stored.Error!);
        }

        var now = _timeProvider.GetUtcNow();
        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            Category = request.Category,
            OriginalFileName = Path.GetFileName(request.OriginalFileName),
            StoredFileName = stored.Value!.StoredFileName,
            ContentType = request.ContentType,
            FileExtension = stored.Value.FileExtension,
            FileSizeBytes = stored.Value.FileSizeBytes,
            StorageRelativePath = stored.Value.StorageRelativePath,
            UploadedByEmployeeId = requesterEmployeeId,
            UploadedAt = now
        };

        try
        {
            await _documentRepository.AddAsync(document, ct);
            await _auditWriter.WriteAsync("EmployeeDocument", document.Id, AuditActionType.DocumentUploaded, requesterEmployeeId, null, ["Document"], null, new { employeeId, documentId = document.Id }, "Document content and storage path redacted.", ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await _storage.DeleteAsync(stored.Value.StorageRelativePath, CancellationToken.None);
            throw;
        }

        return Result<EmployeeDocumentResponse>.Success(Map(document));
    }

    public async Task<Result<PagedList<EmployeeDocumentResponse>>> ListAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentQueryRequest request, CancellationToken ct)
    {
        var auth = await AuthorizeAsync(requesterEmployeeId, employeeId, ct);
        if (auth is not null)
        {
            return Result<PagedList<EmployeeDocumentResponse>>.Failure(auth);
        }

        var page = await _documentRepository.GetCurrentPageAsync(employeeId, request.Page, request.PageSize, ct);
        return Result<PagedList<EmployeeDocumentResponse>>.Success(
            new PagedList<EmployeeDocumentResponse>(page.Items.Select(Map).ToList(), page.TotalCount, page.Page, page.PageSize));
    }

    public async Task<Result<EmployeeDocumentDownload>> DownloadAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var auth = await AuthorizeAsync(requesterEmployeeId, employeeId, ct);
        if (auth is not null)
        {
            return Result<EmployeeDocumentDownload>.Failure(auth);
        }

        var document = await _documentRepository.GetCurrentByIdAsync(employeeId, documentId, ct);
        if (document is null)
        {
            return Result<EmployeeDocumentDownload>.Failure(ServiceError.NotFound("Document was not found."));
        }

        var stream = await _storage.OpenReadAsync(document.StorageRelativePath, ct);
        if (stream is null)
        {
            return Result<EmployeeDocumentDownload>.Failure(ServiceError.NotFound("Document content was not found."));
        }

        return Result<EmployeeDocumentDownload>.Success(new EmployeeDocumentDownload(stream, document.OriginalFileName, document.ContentType));
    }

    public async Task<Result> RemoveAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var auth = await AuthorizeAsync(requesterEmployeeId, employeeId, ct);
        if (auth is not null)
        {
            return Result.Failure(auth);
        }

        var document = await _documentRepository.GetCurrentByIdAsync(employeeId, documentId, ct);
        if (document is null)
        {
            return Result.Failure(ServiceError.NotFound("Document was not found."));
        }

        document.RemovedAt = _timeProvider.GetUtcNow();
        document.RemovedByEmployeeId = requesterEmployeeId;
        await _storage.DeleteAsync(document.StorageRelativePath, ct);
        await _auditWriter.WriteAsync("EmployeeDocument", document.Id, AuditActionType.DocumentRemoved, requesterEmployeeId, null, ["RemovedAt", "RemovedByEmployeeId"], null, new { employeeId, documentId }, "Document content and storage path redacted.", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<ServiceError?> AuthorizeAsync(Guid requesterEmployeeId, Guid employeeId, CancellationToken ct)
    {
        if (!await _accessService.HasAnyRoleAsync(requesterEmployeeId, ct, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator))
        {
            return ServiceError.Forbidden();
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return employee is null || employee.IsDeleted
            ? ServiceError.NotFound($"Employee '{employeeId}' was not found.")
            : null;
    }

    private static EmployeeDocumentResponse Map(EmployeeDocument document)
    {
        return new EmployeeDocumentResponse
        {
            Id = document.Id,
            EmployeeId = document.EmployeeId,
            Category = document.Category,
            OriginalFileName = document.OriginalFileName,
            FileSizeBytes = document.FileSizeBytes,
            ContentType = document.ContentType,
            UploadedByEmployeeId = document.UploadedByEmployeeId,
            UploadedAt = document.UploadedAt
        };
    }
}
