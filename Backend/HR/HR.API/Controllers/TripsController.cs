using HR.API.Extensions;
using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TripsController(ITripService tripService) : ControllerBase
{
    private readonly ITripService _tripService = tripService;

    [HttpGet]
    public async Task<ActionResult<PagedList<TripResponse>>> GetTrips(
        [FromQuery] Guid? travelerEmployeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Invalid session."));
        }

        var result = await _tripService.GetTripsAsync(requesterId.Value, travelerEmployeeId, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripResponse>> GetTrip(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Invalid session."));
        }

        var result = await _tripService.GetTripByIdAsync(requesterId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<TripResponse>> CreateTrip(
        [FromBody] TripCreateRequest request,
        CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Invalid session."));
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tripService.CreateTripAsync(requesterId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetTrip), new { id = result.Value!.Id }, result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTrip(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetEmployeeId();
        if (requesterId is null)
        {
            return this.ToActionResult(ServiceError.Unauthorized("Invalid session."));
        }

        var result = await _tripService.DeleteTripAsync(requesterId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
