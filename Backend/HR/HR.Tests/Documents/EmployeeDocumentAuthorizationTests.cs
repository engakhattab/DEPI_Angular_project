using HR.Application.Documents;
using HR.Application.DTOs.Documents;
using HR.Domain.Enums;
using HR.Infrastructure.Documents;
using HR.Infrastructure.Repositories;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Documents;

public class EmployeeDocumentAuthorizationTests
{
    [Theory]
    [InlineData(EmployeeRole.Employee, false)]
    [InlineData(EmployeeRole.Manager, false)]
    [InlineData(EmployeeRole.HRAdministrator, true)]
    [InlineData(EmployeeRole.SystemAdministrator, true)]
    public async Task ListAsync_AllowsOnlyHrAndSystemAdministrators(EmployeeRole requesterRole, bool shouldSucceed)
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync(
            $"DOC-AUTH-{requesterRole}-{Guid.NewGuid():N}"[..20],
            $"doc-auth-{requesterRole.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            environment.DefaultDepartment!.Id,
            role: requesterRole);
        var employee = await environment.AddEmployeeAsync($"DOC-TGT-{Guid.NewGuid():N}"[..18], $"doc-target-{Guid.NewGuid():N}@example.com", environment.DefaultDepartment.Id);
        var service = CreateService(environment, new NoOpStorage());

        var result = await service.ListAsync(requester.Id, employee.Id, new EmployeeDocumentQueryRequest(), CancellationToken.None);

        Assert.Equal(shouldSucceed, result.IsSuccess);
        if (!shouldSucceed)
        {
            Assert.Equal("FORBIDDEN", result.Error!.Code);
        }
    }

    [Fact]
    public async Task UploadAsync_WhenRequesterIsNotAuthorized_DoesNotSaveFileOrMetadata()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("DOC-AUTH-EMP", "doc-auth-employee@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.Employee);
        var employee = await environment.AddEmployeeAsync("DOC-AUTH-TGT", "doc-auth-target@example.com", environment.DefaultDepartment.Id);
        var storage = new NoOpStorage();
        var service = CreateService(environment, storage);

        var result = await service.UploadAsync(
            requester.Id,
            employee.Id,
            new EmployeeDocumentUploadRequest
            {
                Content = new MemoryStream([1, 2, 3]),
                OriginalFileName = "contract.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 3
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal(0, storage.SaveCount);
        Assert.Empty(await environment.Context.EmployeeDocuments.ToListAsync());
    }

    [Fact]
    public async Task ListAsync_WhenTargetEmployeeIsSoftDeleted_ReturnsNotFound()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("DOC-AUTH-HR", "doc-auth-hr@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var employee = await environment.AddEmployeeAsync("DOC-AUTH-DEL", "doc-auth-deleted@example.com", environment.DefaultDepartment.Id, isDeleted: true);
        var service = CreateService(environment, new NoOpStorage());

        var result = await service.ListAsync(requester.Id, employee.Id, new EmployeeDocumentQueryRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task DownloadAsync_WhenDocumentBelongsToAnotherEmployee_ReturnsNotFound()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync(seedDefaultDepartment: true);
        var requester = await environment.AddEmployeeAsync("DOC-AUTH-SYS", "doc-auth-system@example.com", environment.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var employee = await environment.AddEmployeeAsync("DOC-AUTH-OWN", "doc-auth-owner@example.com", environment.DefaultDepartment.Id);
        var other = await environment.AddEmployeeAsync("DOC-AUTH-OTH", "doc-auth-other@example.com", environment.DefaultDepartment.Id);
        environment.Context.EmployeeDocuments.Add(new HR.Domain.Entities.EmployeeDocument
        {
            EmployeeId = other.Id,
            Category = EmployeeDocumentCategory.Contract,
            OriginalFileName = "other.pdf",
            StoredFileName = "other.pdf",
            ContentType = "application/pdf",
            FileExtension = ".pdf",
            FileSizeBytes = 4,
            StorageRelativePath = "other.pdf",
            UploadedByEmployeeId = requester.Id,
            UploadedAt = DateTimeOffset.UtcNow
        });
        await environment.Context.SaveChangesAsync();
        var documentId = await environment.Context.EmployeeDocuments.Select(d => d.Id).SingleAsync();
        var service = CreateService(environment, new NoOpStorage());

        var result = await service.DownloadAsync(requester.Id, employee.Id, documentId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    private static EmployeeDocumentService CreateService(SqliteTestEnvironment environment, IEmployeeDocumentStorage storage)
    {
        return new EmployeeDocumentService(
            environment.GetRequiredService<IEmployeeDocumentRepository>(),
            storage,
            environment.GetRequiredService<IEmployeeRepository>(),
            environment.GetRequiredService<HR.Application.Authorization.IEmployeeAccessService>(),
            environment.GetRequiredService<HR.Infrastructure.Audit.IAuditWriter>(),
            environment.GetRequiredService<IUnitOfWork>(),
            environment.GetRequiredService<TimeProvider>());
    }

    private sealed class NoOpStorage : IEmployeeDocumentStorage
    {
        public int SaveCount { get; private set; }
        public Task<HR.Shared.Results.Result<StoredEmployeeDocument>> SaveAsync(Stream content, string originalFileName, string contentType, long fileSizeBytes, CancellationToken ct)
        {
            SaveCount++;
            return Task.FromResult(HR.Shared.Results.Result<StoredEmployeeDocument>.Success(new StoredEmployeeDocument("stored.pdf", "stored.pdf", ".pdf", fileSizeBytes)));
        }

        public Task<Stream?> OpenReadAsync(string storageRelativePath, CancellationToken ct) => Task.FromResult<Stream?>(new MemoryStream([1, 2, 3]));
        public Task DeleteAsync(string storageRelativePath, CancellationToken ct) => Task.CompletedTask;
    }
}
