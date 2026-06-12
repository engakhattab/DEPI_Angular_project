using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.Documents;
using HR.Application.DTOs.Documents;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Documents;

public class EmployeeDocumentsControllerTests
{
    [Fact]
    public async Task Upload_ReadsRequesterClaimAndReturnsCreated()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var service = new RecordingDocumentService
        {
            UploadResult = Result<EmployeeDocumentResponse>.Success(new EmployeeDocumentResponse
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Category = EmployeeDocumentCategory.Contract,
                OriginalFileName = "contract.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 3,
                UploadedByEmployeeId = requesterId,
                UploadedAt = DateTimeOffset.UtcNow
            })
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.Upload(employeeId, EmployeeDocumentCategory.Contract, FormFile("contract.pdf", [1, 2, 3]), CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.IsType<EmployeeDocumentResponse>(created.Value);
        Assert.Equal(requesterId, service.UploadRequesterId);
        Assert.Equal(employeeId, service.UploadEmployeeId);
        Assert.Equal("contract.pdf", service.UploadRequest!.OriginalFileName);
        Assert.Equal(EmployeeDocumentCategory.Contract, service.UploadRequest.Category);
    }

    [Fact]
    public async Task Upload_WhenOversized_Returns413StructuredPayloadWithoutCallingService()
    {
        var service = new RecordingDocumentService();
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Upload(Guid.NewGuid(), EmployeeDocumentCategory.Contract, FormFile("large.pdf", [], 10485761), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(objectResult.Value));
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, objectResult.StatusCode);
        Assert.Equal("PAYLOAD_TOO_LARGE", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Uploaded document exceeds the maximum file size.", payload.RootElement.GetProperty("message").GetString());
        Assert.Equal(0, service.UploadCallCount);
    }

    [Fact]
    public async Task List_ReadsRequesterClaimAndPassesPagination()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var service = new RecordingDocumentService
        {
            ListResult = Result<PagedList<EmployeeDocumentResponse>>.Success(new PagedList<EmployeeDocumentResponse>([], 0, 2, 10))
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.List(employeeId, page: 2, pageSize: 10, ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedList<EmployeeDocumentResponse>>(ok.Value);
        Assert.Equal(requesterId, service.ListRequesterId);
        Assert.Equal(employeeId, service.ListEmployeeId);
        Assert.Equal(2, service.ListRequest!.Page);
        Assert.Equal(10, service.ListRequest.PageSize);
    }

    [Fact]
    public async Task Download_WhenDocumentExists_ReturnsFile()
    {
        var service = new RecordingDocumentService
        {
            DownloadResult = Result<EmployeeDocumentDownload>.Success(new EmployeeDocumentDownload(new MemoryStream([1, 2, 3]), "contract.pdf", "application/pdf"))
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Download(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("contract.pdf", file.FileDownloadName);
    }

    [Fact]
    public async Task Download_WhenRemovedDocumentIsRejected_ReturnsStructuredNotFoundPayload()
    {
        var service = new RecordingDocumentService
        {
            DownloadResult = Result<EmployeeDocumentDownload>.Failure(ServiceError.NotFound("Document was not found."))
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Download(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(notFound.Value));
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Document was not found.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var service = new RecordingDocumentService { RemoveResult = Result.Success() };
        var controller = CreateController(service, requesterId);

        var result = await controller.Delete(employeeId, documentId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(requesterId, service.RemoveRequesterId);
        Assert.Equal(employeeId, service.RemoveEmployeeId);
        Assert.Equal(documentId, service.RemoveDocumentId);
    }

    [Theory]
    [InlineData("upload")]
    [InlineData("list")]
    [InlineData("download")]
    [InlineData("delete")]
    public async Task DocumentEndpoints_WhenEmployeeClaimIsMissing_ReturnUnauthorizedStructuredPayload(string endpoint)
    {
        var controller = new EmployeeDocumentsController(new RecordingDocumentService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        object? actionResult = endpoint switch
        {
            "upload" => (await controller.Upload(Guid.NewGuid(), EmployeeDocumentCategory.Contract, FormFile("contract.pdf", [1]), CancellationToken.None)).Result,
            "list" => (await controller.List(Guid.NewGuid(), ct: CancellationToken.None)).Result,
            "download" => await controller.Download(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None),
            _ => await controller.Delete(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None)
        };

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    private static EmployeeDocumentsController CreateController(IEmployeeDocumentService service, Guid requesterId)
    {
        return new EmployeeDocumentsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", requesterId.ToString())
                    ], "test"))
                }
            }
        };
    }

    private static IFormFile FormFile(string fileName, byte[] content, long? length = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, length ?? content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class RecordingDocumentService : IEmployeeDocumentService
    {
        public int UploadCallCount { get; private set; }
        public Guid UploadRequesterId { get; private set; }
        public Guid UploadEmployeeId { get; private set; }
        public EmployeeDocumentUploadRequest? UploadRequest { get; private set; }
        public Result<EmployeeDocumentResponse>? UploadResult { get; init; }
        public Guid ListRequesterId { get; private set; }
        public Guid ListEmployeeId { get; private set; }
        public EmployeeDocumentQueryRequest? ListRequest { get; private set; }
        public Result<PagedList<EmployeeDocumentResponse>>? ListResult { get; init; }
        public Result<EmployeeDocumentDownload>? DownloadResult { get; init; }
        public Guid RemoveRequesterId { get; private set; }
        public Guid RemoveEmployeeId { get; private set; }
        public Guid RemoveDocumentId { get; private set; }
        public Result? RemoveResult { get; init; }

        public Task<Result<EmployeeDocumentResponse>> UploadAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentUploadRequest request, CancellationToken ct)
        {
            UploadCallCount++;
            UploadRequesterId = requesterEmployeeId;
            UploadEmployeeId = employeeId;
            UploadRequest = request;
            return Task.FromResult(UploadResult ?? throw new NotSupportedException());
        }

        public Task<Result<PagedList<EmployeeDocumentResponse>>> ListAsync(Guid requesterEmployeeId, Guid employeeId, EmployeeDocumentQueryRequest request, CancellationToken ct)
        {
            ListRequesterId = requesterEmployeeId;
            ListEmployeeId = employeeId;
            ListRequest = request;
            return Task.FromResult(ListResult ?? throw new NotSupportedException());
        }

        public Task<Result<EmployeeDocumentDownload>> DownloadAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct)
            => Task.FromResult(DownloadResult ?? throw new NotSupportedException());

        public Task<Result> RemoveAsync(Guid requesterEmployeeId, Guid employeeId, Guid documentId, CancellationToken ct)
        {
            RemoveRequesterId = requesterEmployeeId;
            RemoveEmployeeId = employeeId;
            RemoveDocumentId = documentId;
            return Task.FromResult(RemoveResult ?? throw new NotSupportedException());
        }
    }
}
