namespace HR.Infrastructure.Configuration;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";
    public string RootPath { get; set; } = string.Empty;
    public string[] AllowedExtensions { get; set; } = [];
    public long MaxFileSizeBytes { get; set; }
}
