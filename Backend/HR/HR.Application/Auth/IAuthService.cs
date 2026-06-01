using HR.Domain.Entities;
using HR.Shared.Results;

namespace HR.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthenticatedEmployee>> ValidateCredentialsAsync(
        string identifier,
        string password,
        CancellationToken ct);
}

public sealed record AuthenticatedEmployee(
    Employee Employee,
    string UserId,
    string? UserName,
    string? UserEmail);
