using System.Text;
using HR.Application.Authorization;
using HR.Application.Documents;
using HR.Application.DTOs.Documents;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Documents;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Documents;

public class EmployeeDocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenValid_SavesFileMetadataAndAuditWithSanitizedOriginalFileName()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true, timeProvider: new TestTimeProvider(now));
        var requester = await environment.AddEmployeeAsync("DOC-HR-001", "doc-hr-1@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("DOC-EMP-001", "doc-employee-1@example.com", environment.DefaultDepartment.Id);
        var storage = new FakeDocumentStorage();
        var service = CreateService(environment, storage);

        var result = await service.UploadAsync(
            requester.Id,
            employee.Id,
            UploadRequest(@"..\contract.PDF", "contract-content"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(employee.Id, result.Value!.EmployeeId);
        Assert.Equal("contract.PDF", result.Value.OriginalFileName);
        Assert.Equal(now, result.Value.UploadedAt);
        Assert.Equal(requester.Id, result.Value.UploadedByEmployeeId);
        Assert.Single(storage.Files);

        var document = await environment.Context.EmployeeDocuments.SingleAsync();
        Assert.Equal("contract.PDF", document.OriginalFileName);
        Assert.Equal(".pdf", document.FileExtension);
        Assert.NotEqual(document.OriginalFileName, document.StoredFileName);
        var audit = await environment.Context.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionType.DocumentUploaded, audit.ActionType);
        Assert.Equal("Document content and storage path redacted.", audit.SensitiveSummary);
        Assert.DoesNotContain(document.StorageRelativePath, audit.NewValues ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_WhenMetadataSaveFails_CleansUpUploadedFile()
    {
        var storage = new FakeDocumentStorage();
        var service = new EmployeeDocumentService(
            new InMemoryDocumentRepository(),
            storage,
            new StubEmployeeRepository(new Employee { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") }),
            new AllowingAccessService(),
            new NoOpAuditWriter(),
            new ThrowingUnitOfWork(),
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(
                Guid.NewGuid(),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UploadRequest("contract.pdf", "contract-content"),
                CancellationToken.None));

        Assert.Single(storage.DeletedPaths);
    }

    [Fact]
    public async Task UploadAsync_WhenFileSaveFails_DoesNotCreateMetadata()
    {
        var storage = new FakeDocumentStorage
        {
            SaveResult = Result<StoredEmployeeDocument>.Failure(ServiceError.BusinessRule("Document file type is not allowed."))
        };
        var repository = new InMemoryDocumentRepository();
        var service = new EmployeeDocumentService(
            repository,
            storage,
            new StubEmployeeRepository(new Employee { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") }),
            new AllowingAccessService(),
            new NoOpAuditWriter(),
            new CountingUnitOfWork(),
            TimeProvider.System);

        var result = await service.UploadAsync(
            Guid.NewGuid(),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UploadRequest("contract.exe", "contract-content"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task ListDownloadAndRemoveAsync_OnlyShowsCurrentDocumentsAndDeletesRemovedFile()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("DOC-HR-002", "doc-hr-2@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var employee = await environment.AddEmployeeAsync("DOC-EMP-002", "doc-employee-2@example.com", environment.DefaultDepartment.Id);
        var storage = new FakeDocumentStorage();
        var service = CreateService(environment, storage);
        var first = await service.UploadAsync(requester.Id, employee.Id, UploadRequest("first.pdf", "first"), CancellationToken.None);
        var second = await service.UploadAsync(requester.Id, employee.Id, UploadRequest("second.pdf", "second"), CancellationToken.None);

        var beforeRemove = await service.ListAsync(requester.Id, employee.Id, new EmployeeDocumentQueryRequest(), CancellationToken.None);
        var download = await service.DownloadAsync(requester.Id, employee.Id, first.Value!.Id, CancellationToken.None);
        var remove = await service.RemoveAsync(requester.Id, employee.Id, first.Value.Id, CancellationToken.None);
        var afterRemove = await service.ListAsync(requester.Id, employee.Id, new EmployeeDocumentQueryRequest(), CancellationToken.None);
        var removedDownload = await service.DownloadAsync(requester.Id, employee.Id, first.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.True(beforeRemove.IsSuccess, beforeRemove.Error?.Message);
        Assert.Equal(2, beforeRemove.Value!.TotalCount);
        Assert.True(download.IsSuccess, download.Error?.Message);
        await using (download.Value!.Content)
        using (var reader = new StreamReader(download.Value.Content))
        {
            Assert.Equal("first", await reader.ReadToEndAsync());
        }

        Assert.True(remove.IsSuccess);
        Assert.True(afterRemove.IsSuccess, afterRemove.Error?.Message);
        var current = Assert.Single(afterRemove.Value!.Items);
        Assert.Equal(second.Value!.Id, current.Id);
        Assert.True(removedDownload.IsFailure);
        Assert.Equal("NOT_FOUND", removedDownload.Error!.Code);

        var firstDocument = await environment.Context.EmployeeDocuments.IgnoreQueryFilters().SingleAsync(d => d.Id == first.Value.Id);
        var secondDocument = await environment.Context.EmployeeDocuments.SingleAsync(d => d.Id == second.Value.Id);
        Assert.NotNull(firstDocument.RemovedAt);
        Assert.Equal(requester.Id, firstDocument.RemovedByEmployeeId);
        Assert.Null(secondDocument.RemovedAt);
        Assert.Contains(firstDocument.StorageRelativePath, storage.DeletedPaths);
        Assert.DoesNotContain(secondDocument.StorageRelativePath, storage.DeletedPaths);
        Assert.Contains(await environment.Context.AuditLogEntries.ToListAsync(), a => a.ActionType == AuditActionType.DocumentRemoved);
    }

    private static EmployeeDocumentService CreateService(SqliteTestEnvironment environment, IEmployeeDocumentStorage storage)
    {
        return new EmployeeDocumentService(
            environment.GetRequiredService<IEmployeeDocumentRepository>(),
            storage,
            environment.GetRequiredService<IEmployeeRepository>(),
            environment.GetRequiredService<IEmployeeAccessService>(),
            environment.GetRequiredService<IAuditWriter>(),
            environment.GetRequiredService<IUnitOfWork>(),
            environment.GetRequiredService<TimeProvider>());
    }

    private static EmployeeDocumentUploadRequest UploadRequest(string originalFileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new EmployeeDocumentUploadRequest
        {
            Category = EmployeeDocumentCategory.Contract,
            Content = new MemoryStream(bytes),
            OriginalFileName = originalFileName,
            ContentType = "application/pdf",
            FileSizeBytes = bytes.Length
        };
    }

    private sealed class FakeDocumentStorage : IEmployeeDocumentStorage
    {
        private int _counter;

        public Dictionary<string, byte[]> Files { get; } = [];

        public List<string> DeletedPaths { get; } = [];

        public Result<StoredEmployeeDocument>? SaveResult { get; init; }

        public async Task<Result<StoredEmployeeDocument>> SaveAsync(Stream content, string originalFileName, string contentType, long fileSizeBytes, CancellationToken ct)
        {
            if (SaveResult is not null)
            {
                return SaveResult;
            }

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"stored-{++_counter}{extension}";
            await using var copy = new MemoryStream();
            await content.CopyToAsync(copy, ct);
            Files[storedFileName] = copy.ToArray();
            return Result<StoredEmployeeDocument>.Success(new StoredEmployeeDocument(storedFileName, storedFileName, extension, fileSizeBytes));
        }

        public Task<Stream?> OpenReadAsync(string storageRelativePath, CancellationToken ct)
        {
            return Task.FromResult<Stream?>(Files.TryGetValue(storageRelativePath, out var bytes) && !DeletedPaths.Contains(storageRelativePath)
                ? new MemoryStream(bytes)
                : null);
        }

        public Task DeleteAsync(string storageRelativePath, CancellationToken ct)
        {
            DeletedPaths.Add(storageRelativePath);
            Files.Remove(storageRelativePath);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryDocumentRepository : IEmployeeDocumentRepository
    {
        public int AddCount { get; private set; }

        public Task<EmployeeDocument?> GetCurrentByIdAsync(Guid employeeId, Guid documentId, CancellationToken ct)
            => Task.FromResult<EmployeeDocument?>(null);

        public Task<PagedList<EmployeeDocument>> GetCurrentPageAsync(Guid employeeId, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedList<EmployeeDocument>([], 0, page, pageSize));

        public Task AddAsync(EmployeeDocument document, CancellationToken ct)
        {
            AddCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmployeeRepository(Employee? employee) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(employee);
        public Task<PagedList<Employee>> GetPageWithDetailsAsync(EmployeeStatus? status, int page, int pageSize, CancellationToken ct) => throw new NotSupportedException();
        public Task<Employee?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Employee?> GetByApplicationUserIdWithDetailsAsync(string applicationUserId, CancellationToken ct) => throw new NotSupportedException();
        public Task<Employee?> GetByEmployeeNumberWithDetailsAsync(string employeeNumber, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Employee>> FindByEmailOrEmployeeNumberAsync(string identifier, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Employee>> GetAllActiveAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlySet<Guid>> GetDirectAndIndirectReportIdsAsync(Guid managerId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> AnyActiveSystemAdministratorAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExistsActiveWithEmailAsync(string email, Guid? excludingEmployeeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid?> GetManagerIdAsync(Guid employeeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> IsAuthenticationEligibleAsync(Guid employeeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExistsByNumberAsync(string employeeNumber, CancellationToken ct) => throw new NotSupportedException();
        public Task AddAsync(Employee employee, CancellationToken ct) => throw new NotSupportedException();
        public void Remove(Employee employee) => throw new NotSupportedException();
    }

    private sealed class AllowingAccessService : IEmployeeAccessService
    {
        public Task<Result<EmployeeAccessContext>> GetCurrentAsync(Guid employeeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> HasAnyRoleAsync(Guid employeeId, CancellationToken ct, params EmployeeRole[] roles) => Task.FromResult(true);
        public Task<bool> CanAccessEmployeeAsync(Guid requesterEmployeeId, Guid targetEmployeeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlySet<Guid>> GetVisibleEmployeeIdsAsync(Guid requesterEmployeeId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class NoOpAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string entityType, Guid entityId, AuditActionType actionType, Guid? actorEmployeeId, string? actorMarker, IReadOnlyList<string> changedFields, object? oldValues, object? newValues, string? sensitiveSummary, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct) => throw new InvalidOperationException("metadata save failed");
        public Task ExecuteWithStrategyAsync(Func<CancellationToken, Task> operation, CancellationToken ct) => operation(ct);
        public Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(0);
        public Task ExecuteWithStrategyAsync(Func<CancellationToken, Task> operation, CancellationToken ct) => operation(ct);
        public Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct) => throw new NotSupportedException();
    }
}
