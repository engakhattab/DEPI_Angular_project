using HR.API.Documentation;
using HR.API.Extensions;
using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VacationRequestsController(IVacationRequestService vacationRequestService) : ControllerBase
{
    private readonly IVacationRequestService _vacationRequestService = vacationRequestService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<VacationRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVacationRequests(
        [FromQuery] VacationRequestStatus? status = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var result = await _vacationRequestService.GetVacationRequestsAsync(
            requesterId.Value,
            status,
            employeeId,
            page,
            pageSize,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var result = await _vacationRequestService.GetVacationRequestByIdAsync(
            requesterId.Value,
            id,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateVacationRequest(
        [FromBody] VacationRequestCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var result = await _vacationRequestService.CreateVacationRequestAsync(
            requesterId.Value,
            request,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetVacationRequest), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateVacationStatus(
        Guid id,
        [FromBody] VacationRequestStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var reviewerClaim = User.FindFirstValue("employee_id");
        if (!Guid.TryParse(reviewerClaim, out var reviewerEmployeeId))
        {
            return this.ToActionResult(ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var result = await _vacationRequestService.UpdateVacationStatusAsync(id, reviewerEmployeeId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDocumentation), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteVacationRequest(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Authenticated employee context is invalid."));
        }

        var result = await _vacationRequestService.DeleteVacationRequestAsync(
            requesterId.Value,
            id,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
