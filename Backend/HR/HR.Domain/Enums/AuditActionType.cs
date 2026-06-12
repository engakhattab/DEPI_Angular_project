namespace HR.Domain.Enums;

public enum AuditActionType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    StatusChanged = 3,
    RoleChanged = 4,
    ClockedIn = 5,
    ClockedOut = 6,
    CompensationChanged = 7,
    DocumentUploaded = 8,
    DocumentRemoved = 9,
    SystemAdministratorBootstrapped = 10,
    InitialAdminCreated = 11
}
