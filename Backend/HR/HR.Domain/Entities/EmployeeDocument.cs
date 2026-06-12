using HR.Domain.Enums;

namespace HR.Domain.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public EmployeeDocumentCategory Category { get; set; } = EmployeeDocumentCategory.Other;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StorageRelativePath { get; set; } = string.Empty;
    public Guid UploadedByEmployeeId { get; set; }
    public Employee? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RemovedAt { get; set; }
    public Guid? RemovedByEmployeeId { get; set; }
    public Employee? RemovedBy { get; set; }
}
