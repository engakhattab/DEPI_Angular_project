using HR.Domain.Enums;

namespace HR.Application.DTOs.Documents;

public class EmployeeDocumentUploadRequest
{
    public EmployeeDocumentCategory Category { get; set; } = EmployeeDocumentCategory.Other;
    public Stream Content { get; set; } = Stream.Null;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
}
