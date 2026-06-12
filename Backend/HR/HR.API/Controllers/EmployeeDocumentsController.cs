using HR.API.Extensions;
using HR.Application.Documents;
using HR.Application.DTOs.Documents;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[ApiController]
[Authorize(Policy = "HRAdministrator")]
[Route("api/employees/{employeeId:guid}/documents")]
public class EmployeeDocumentsController(IEmployeeDocumentService documentService) : ControllerBase
{
    private readonly IEmployeeDocumentService _documentService = documentService;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10485760)]
    public async Task<ActionResult<EmployeeDocumentResponse>> Upload(
        Guid employeeId,
        [FromForm] EmployeeDocumentCategory category,
        IFormFile file,
        CancellationToken ct)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        if (file.Length > 10485760)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { code = "PAYLOAD_TOO_LARGE", message = "Uploaded document exceeds the maximum file size." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            requesterId.Value,
            employeeId,
            new EmployeeDocumentUploadRequest
            {
                Category = category,
                Content = stream,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length
            },
            ct);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<EmployeeDocumentResponse>>> List(Guid employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _documentService.ListAsync(requesterId.Value, employeeId, new EmployeeDocumentQueryRequest { Page = page, PageSize = pageSize }, ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Download(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _documentService.DownloadAsync(requesterId.Value, employeeId, documentId, ct);
        return result.IsSuccess
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
            : this.ToActionResult(result.Error!);
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _documentService.RemoveAsync(requesterId.Value, employeeId, documentId, ct);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
