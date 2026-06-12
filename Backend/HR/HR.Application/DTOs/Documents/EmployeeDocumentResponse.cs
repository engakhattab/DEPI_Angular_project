using HR.Domain.Enums;

namespace HR.Application.DTOs.Documents;

public class EmployeeDocumentResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public EmployeeDocumentCategory Category { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid UploadedByEmployeeId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
