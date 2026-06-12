namespace HR.Infrastructure.Configuration;

public class InitialAdminBootstrapOptions
{
    public const string SectionName = "InitialAdminBootstrap";
    public const string CreateInitialAdminMode = "CreateInitialAdmin";

    public bool Enabled { get; set; }
    public string Mode { get; set; } = CreateInitialAdminMode;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool ForcePasswordChange { get; set; }
}
