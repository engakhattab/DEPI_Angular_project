using HR.API.Extensions;
using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VacationRequestsController(IVacationRequestService vacationRequestService) : ControllerBase
{
    private readonly IVacationRequestService _vacationRequestService = vacationRequestService;

    [HttpGet]
    public async Task<ActionResult<PagedList<VacationRequestResponse>>> GetVacationRequests(
        [FromQuery] VacationRequestStatus? status = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _vacationRequestService.GetVacationRequestsAsync(
            status,
            employeeId,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VacationRequestResponse>> GetVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var request = await _vacationRequestService.GetVacationRequestByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return this.ToActionResult(ServiceError.NotFound($"Vacation request '{id}' was not found.", "NOT_FOUND"));
        }

        return Ok(request);
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

        var result = await _vacationRequestService.CreateVacationRequestAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetVacationRequest), new { id = result.Value!.Id }, result.Value);
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

        var result = await _vacationRequestService.UpdateVacationStatusAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vacationRequestService.DeleteVacationRequestAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
