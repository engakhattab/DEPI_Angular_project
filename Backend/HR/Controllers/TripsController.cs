using HR.Data;
using HR.DTOs.Transportation;
using HR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController(ApplicationDbContext context, ILogger<TripsController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<TripsController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripResponse>>> GetTrips(CancellationToken cancellationToken)
    {
        var trips = await _context.Trips
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return trips.Select(TripResponse.FromEntity).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripResponse>> GetTrip(Guid id, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (trip is null)
        {
            return NotFound();
        }

        return TripResponse.FromEntity(trip);
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

        var trip = new Trip
        {
            ReferenceName = request.ReferenceName,
            Project = request.Project,
            Route = request.Route,
            TripType = request.TripType,
            TripDate = request.TripDate,
            TripCode = GenerateCode("TRIP"),
            RequestCode = GenerateCode("REQ"),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(cancellationToken);

        var response = TripResponse.FromEntity(trip);
        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTrip(Guid id, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string GenerateCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
