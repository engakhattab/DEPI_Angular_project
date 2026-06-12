using HR.Application.Authorization;
using HR.Application.Compensation;
using HR.Application.DTOs.Compensation;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Audit;
using HR.Infrastructure.Repositories;
using HR.Shared.Results;

namespace HR.Infrastructure.Compensation;

public class CompensationService(
    ICompensationRepository compensationRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeAccessService accessService,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICompensationService
{
    private readonly ICompensationRepository _compensationRepository = compensationRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IEmployeeAccessService _accessService = accessService;
    private readonly IAuditWriter _auditWriter = auditWriter;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<CompensationResponse>> GetAsync(Guid requesterEmployeeId, Guid employeeId, CancellationToken ct)
    {
        if (!await IsAuthorizedAsync(requesterEmployeeId, ct))
        {
            return Result<CompensationResponse>.Failure(ServiceError.Forbidden());
        }

        if (!await _employeeRepository.ExistsAsync(employeeId, ct))
        {
            return Result<CompensationResponse>.Failure(ServiceError.NotFound($"Employee '{employeeId}' was not found."));
        }

        return Result<CompensationResponse>.Success(await MapAsync(employeeId, ct));
    }

    public async Task<Result<CompensationResponse>> UpdateAsync(Guid requesterEmployeeId, Guid employeeId, CompensationUpdateRequest request, CancellationToken ct)
    {
        if (!await IsAuthorizedAsync(requesterEmployeeId, ct))
        {
            return Result<CompensationResponse>.Failure(ServiceError.Forbidden());
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        if (employee is null || employee.IsDeleted)
        {
            return Result<CompensationResponse>.Failure(ServiceError.NotFound($"Employee '{employeeId}' was not found."));
        }

        if (request.BaseSalary < 0)
        {
            return Result<CompensationResponse>.Failure(ServiceError.BusinessRule("Base salary must be non-negative."));
        }

        var currency = request.SalaryCurrency.Trim().ToUpperInvariant();
        if (currency.Length is < 3 or > 8 || currency.Any(ch => !char.IsLetter(ch)))
        {
            return Result<CompensationResponse>.Failure(ServiceError.BusinessRule("Salary currency is invalid."));
        }

        var now = _timeProvider.GetUtcNow();
        var compensation = await _compensationRepository.GetByEmployeeIdAsync(employeeId, ct);
        if (compensation is null)
        {
            compensation = new EmployeeCompensation
            {
                EmployeeId = employeeId,
                BaseSalary = request.BaseSalary,
                SalaryCurrency = currency,
                LastSalaryReviewDate = request.LastSalaryReviewDate,
                CreatedAt = now
            };

            await _compensationRepository.AddCompensationAsync(compensation, ct);
            await AddHistoryAsync(employeeId, requesterEmployeeId, null, request.BaseSalary, null, currency, null, request.LastSalaryReviewDate, now, ct);
            await _auditWriter.WriteAsync("EmployeeCompensation", employeeId, AuditActionType.CompensationChanged, requesterEmployeeId, null, ["BaseSalary", "SalaryCurrency", "LastSalaryReviewDate"], null, null, "Compensation values changed.", ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<CompensationResponse>.Success(await MapAsync(employeeId, ct));
        }

        var changed = compensation.BaseSalary != request.BaseSalary
            || !string.Equals(compensation.SalaryCurrency, currency, StringComparison.Ordinal)
            || compensation.LastSalaryReviewDate != request.LastSalaryReviewDate;

        if (!changed)
        {
            return Result<CompensationResponse>.Success(await MapAsync(employeeId, ct));
        }

        var previousSalary = compensation.BaseSalary;
        var previousCurrency = compensation.SalaryCurrency;
        var previousReviewDate = compensation.LastSalaryReviewDate;

        compensation.BaseSalary = request.BaseSalary;
        compensation.SalaryCurrency = currency;
        compensation.LastSalaryReviewDate = request.LastSalaryReviewDate;
        compensation.UpdatedAt = now;

        await AddHistoryAsync(employeeId, requesterEmployeeId, previousSalary, request.BaseSalary, previousCurrency, currency, previousReviewDate, request.LastSalaryReviewDate, now, ct);
        await _auditWriter.WriteAsync("EmployeeCompensation", employeeId, AuditActionType.CompensationChanged, requesterEmployeeId, null, ["BaseSalary", "SalaryCurrency", "LastSalaryReviewDate"], null, null, "Compensation values changed.", ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<CompensationResponse>.Success(await MapAsync(employeeId, ct));
    }

    private Task<bool> IsAuthorizedAsync(Guid requesterEmployeeId, CancellationToken ct)
    {
        return _accessService.HasAnyRoleAsync(requesterEmployeeId, ct, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator);
    }

    private async Task<CompensationResponse> MapAsync(Guid employeeId, CancellationToken ct)
    {
        var compensation = await _compensationRepository.GetByEmployeeIdAsync(employeeId, ct);
        var history = await _compensationRepository.GetHistoryAsync(employeeId, ct);

        return new CompensationResponse
        {
            EmployeeId = employeeId,
            BaseSalary = compensation?.BaseSalary,
            SalaryCurrency = compensation?.SalaryCurrency,
            LastSalaryReviewDate = compensation?.LastSalaryReviewDate,
            History = history.Select(h => new SalaryHistoryEntryResponse
            {
                Id = h.Id,
                ChangedAt = h.ChangedAt,
                ChangedByEmployeeId = h.ChangedByEmployeeId,
                PreviousBaseSalary = h.PreviousBaseSalary,
                NewBaseSalary = h.NewBaseSalary,
                PreviousCurrency = h.PreviousCurrency,
                NewCurrency = h.NewCurrency,
                PreviousReviewDate = h.PreviousReviewDate,
                NewReviewDate = h.NewReviewDate
            }).ToList()
        };
    }

    private Task AddHistoryAsync(
        Guid employeeId,
        Guid actorId,
        decimal? previousSalary,
        decimal newSalary,
        string? previousCurrency,
        string newCurrency,
        DateOnly? previousReviewDate,
        DateOnly? newReviewDate,
        DateTimeOffset changedAt,
        CancellationToken ct)
    {
        return _compensationRepository.AddHistoryAsync(new SalaryHistoryEntry
        {
            EmployeeId = employeeId,
            ChangedByEmployeeId = actorId,
            PreviousBaseSalary = previousSalary,
            NewBaseSalary = newSalary,
            PreviousCurrency = previousCurrency,
            NewCurrency = newCurrency,
            PreviousReviewDate = previousReviewDate,
            NewReviewDate = newReviewDate,
            ChangedAt = changedAt
        }, ct);
    }
}
