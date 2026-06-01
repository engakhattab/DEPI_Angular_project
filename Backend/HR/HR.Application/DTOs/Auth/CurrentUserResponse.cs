namespace HR.Application.DTOs.Auth;

public class CurrentUserResponse
{
    public Guid EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
