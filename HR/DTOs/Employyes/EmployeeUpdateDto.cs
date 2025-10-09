namespace HR.DTOs.Employyes
{
    public record EmployeeUpdateDto(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Guid DepartmentId,
    Guid PositionId,
    Guid? ManagerId,
    bool IsActive
);
}
