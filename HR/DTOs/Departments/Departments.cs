namespace HR.DTOs.Departments
{
    public record DepartmentCreateDto(
    string Name,
    string Code,
    Guid? ParentDepartmentId
);
}
