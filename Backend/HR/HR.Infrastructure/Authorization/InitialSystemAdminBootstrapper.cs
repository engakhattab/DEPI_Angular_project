using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Configuration;
using HR.Infrastructure.Identity;
using HR.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HR.Infrastructure.Authorization;

public class InitialSystemAdminBootstrapper(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IOptions<InitialAdminBootstrapOptions> options,
    TimeProvider timeProvider)
{
    public const string SystemActorMarker = "SYSTEM_BOOTSTRAP";

    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IAuditWriter _auditWriter = auditWriter;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly InitialAdminBootstrapOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (await _employeeRepository.AnyActiveSystemAdministratorAsync(ct))
        {
            return;
        }

        ValidateRequiredConfiguration();

        if (!string.Equals(_options.Mode, InitialAdminBootstrapOptions.CreateInitialAdminMode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("InitialAdminBootstrap:Mode must be 'CreateInitialAdmin'.");
        }

        var employeeNumber = _options.EmployeeNumber.Trim();
        var email = _options.Email.Trim();
        var fullName = _options.FullName.Trim();

        if (await _employeeRepository.ExistsByNumberAsync(employeeNumber, ct))
        {
            throw new InvalidOperationException("Initial admin employee number already exists.");
        }

        if (await _employeeRepository.ExistsWithEmailAsync(email, ct) || await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("Initial admin email already exists.");
        }

        var department = await _departmentRepository.GetByIdAsync(_options.DepartmentId!.Value, ct);
        if (department is null)
        {
            throw new InvalidOperationException("InitialAdminBootstrap:DepartmentId must reference an existing department.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var identityResult = await _userManager.CreateAsync(user, _options.TemporaryPassword);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException($"Initial admin identity creation failed: {BuildIdentityErrorMessage(identityResult.Errors)}");
        }

        try
        {
            var employee = new HR.Domain.Entities.Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = employeeNumber,
                FullName = fullName,
                Email = email,
                DepartmentId = department.Id,
                Department = department,
                Status = EmployeeStatus.Active,
                Role = EmployeeRole.SystemAdministrator,
                VacationBalanceDays = 21,
                ApplicationUserId = user.Id
            };

            await _employeeRepository.AddAsync(employee, ct);
            await _auditWriter.WriteAsync(
                "Employee",
                employee.Id,
                AuditActionType.InitialAdminCreated,
                null,
                SystemActorMarker,
                ["EmployeeNumber", "Email", "Role"],
                null,
                new
                {
                    employeeNumber,
                    email,
                    role = EmployeeRole.SystemAdministrator.ToString()
                },
                null,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private void ValidateRequiredConfiguration()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Initial admin bootstrap is disabled and no active System administrator exists.");
        }

        if (string.IsNullOrWhiteSpace(_options.EmployeeNumber))
        {
            throw new InvalidOperationException("InitialAdminBootstrap:EmployeeNumber is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.Email))
        {
            throw new InvalidOperationException("InitialAdminBootstrap:Email is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.FullName))
        {
            throw new InvalidOperationException("InitialAdminBootstrap:FullName is required.");
        }

        if (!_options.DepartmentId.HasValue || _options.DepartmentId == Guid.Empty)
        {
            throw new InvalidOperationException("InitialAdminBootstrap:DepartmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.TemporaryPassword))
        {
            throw new InvalidOperationException("InitialAdminBootstrap:TemporaryPassword is required.");
        }
    }

    private static string BuildIdentityErrorMessage(IEnumerable<IdentityError> errors)
    {
        var messages = errors.Select(e => e.Description).Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();
        return messages.Length == 0 ? "Identity operation failed." : string.Join(" ", messages);
    }
}
