using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.VacationRequests;

public class VacationRequestService(ApplicationDbContext context) : IVacationRequestService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedList<VacationRequestResponse>> GetVacationRequestsAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        IQueryable<VacationRequest> query = _context.VacationRequests
            .AsNoTracking()
            .Include(v => v.Employee)
            .OrderByDescending(v => v.CreatedAt);

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(v => v.EmployeeId == employeeId.Value);
        }

        var pagedEntities = await PagedList<VacationRequest>.CreateAsync(query, page, pageSize, ct);
        var items = pagedEntities.Items.Select(VacationRequestResponse.FromEntity).ToList();

        return new PagedList<VacationRequestResponse>(items, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);
    }

    public async Task<VacationRequestResponse?> GetVacationRequestByIdAsync(Guid id, CancellationToken ct)
    {
        var request = await _context.VacationRequests
            .AsNoTracking()
            .Include(v => v.Employee)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        return request is null ? null : VacationRequestResponse.FromEntity(request);
    }

    public async Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(VacationRequestCreateRequest request, CancellationToken ct)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.Validation("End date must be on or after the start date.", "VALIDATION"));
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);

        if (employee is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Employee '{request.EmployeeId}' was not found.", "NOT_FOUND"));
        }

        var vacationRequest = new VacationRequest
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = VacationRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.VacationRequests.Add(vacationRequest);
        await _context.SaveChangesAsync(ct);

        vacationRequest.Employee = employee;
        return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
    }

    public async Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(
        Guid id,
        VacationRequestStatusUpdateRequest request,
        CancellationToken ct)
    {
        var vacationRequest = await _context.VacationRequests
            .Include(v => v.Employee)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vacationRequest is null)
        {
            return Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        vacationRequest.Status = request.Status;
        vacationRequest.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Result<VacationRequestResponse>.Success(VacationRequestResponse.FromEntity(vacationRequest));
    }

    public async Task<Result> DeleteVacationRequestAsync(Guid id, CancellationToken ct)
    {
        var vacationRequest = await _context.VacationRequests.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vacationRequest is null)
        {
            return Result.Failure(ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        _context.VacationRequests.Remove(vacationRequest);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
