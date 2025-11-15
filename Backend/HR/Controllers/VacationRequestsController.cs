using HR.Data;
using HR.DTOs.VacationRequests;
using HR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VacationRequestsController(ApplicationDbContext context, ILogger<VacationRequestsController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<VacationRequestsController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VacationRequestResponse>>> GetVacationRequests(
        [FromQuery] VacationRequestStatus? status = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
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

        var requests = await query.ToListAsync(cancellationToken);
        return requests.Select(VacationRequestResponse.FromEntity).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VacationRequestResponse>> GetVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var request = await _context.VacationRequests
            .AsNoTracking()
            .Include(v => v.Employee)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        return VacationRequestResponse.FromEntity(request);
    }

    [HttpPost]
    public async Task<ActionResult<VacationRequestResponse>> CreateVacationRequest(
        [FromBody] VacationRequestCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.StartDate > request.EndDate)
        {
            ModelState.AddModelError(nameof(request.EndDate), "End date must be on or after the start date.");
            return ValidationProblem(ModelState);
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound($"Employee '{request.EmployeeId}' was not found.");
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
        await _context.SaveChangesAsync(cancellationToken);

        vacationRequest.Employee = employee;

        return CreatedAtAction(nameof(GetVacationRequest), new { id = vacationRequest.Id }, VacationRequestResponse.FromEntity(vacationRequest));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<VacationRequestResponse>> UpdateVacationStatus(
        Guid id,
        [FromBody] VacationRequestStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vacationRequest = await _context.VacationRequests
            .Include(v => v.Employee)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vacationRequest is null)
        {
            return NotFound();
        }

        vacationRequest.Status = request.Status;
        vacationRequest.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return VacationRequestResponse.FromEntity(vacationRequest);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var vacationRequest = await _context.VacationRequests.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacationRequest is null)
        {
            return NotFound();
        }

        _context.VacationRequests.Remove(vacationRequest);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
