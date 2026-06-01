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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _tripService.GetTripsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripResponse>> GetTrip(Guid id, CancellationToken cancellationToken)
    {
        var trip = await _tripService.GetTripByIdAsync(id, cancellationToken);
        if (trip is null)
        {
            return this.ToActionResult(ServiceError.NotFound($"Trip '{id}' was not found.", "NOT_FOUND"));
        }

        return Ok(trip);
    }

    [HttpPost]
    public async Task<ActionResult<TripResponse>> CreateTrip(
        [FromBody] TripCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tripService.CreateTripAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return CreatedAtAction(nameof(GetTrip), new { id = result.Value!.Id }, result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTrip(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tripService.DeleteTripAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return NoContent();
    }
}
