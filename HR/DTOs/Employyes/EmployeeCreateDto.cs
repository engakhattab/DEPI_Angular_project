namespace HR.DTOs.Employyes
{
    public record EmployeeCreateDto(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateTime HireDate,
    Guid DepartmentId,
    Guid PositionId,
    Guid? ManagerId
);
}
