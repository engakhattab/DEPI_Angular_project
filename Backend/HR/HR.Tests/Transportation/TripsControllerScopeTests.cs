using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Transportation;

public class TripsControllerScopeTests
{
    [Fact]
    public async Task GetTrips_ReadsRequesterClaimAndPassesTravelerFilterAndPagination()
    {
        var requesterId = Guid.NewGuid();
        var travelerId = Guid.NewGuid();
        var service = new RecordingTripService();
        var controller = CreateControllerWithEmployeeClaim(service, requesterId);

        var result = await controller.GetTrips(travelerId, 2, 10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Equal(requesterId, service.GetTripsRequesterEmployeeId);
        Assert.Equal(travelerId, service.GetTripsTravelerEmployeeId);
        Assert.Equal(2, service.GetTripsPage);
        Assert.Equal(10, service.GetTripsPageSize);
    }

    [Fact]
    public async Task CreateTrip_ReadsRequesterClaimAndPassesBodyTraveler()
    {
        var requesterId = Guid.NewGuid();
        var travelerId = Guid.NewGuid();
        var service = new RecordingTripService();
        var controller = CreateControllerWithEmployeeClaim(service, requesterId);
        var request = BuildRequest(travelerId);

        var result = await controller.CreateTrip(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(requesterId, service.CreateRequesterEmployeeId);
        Assert.Same(request, service.CreateRequest);
        Assert.Equal(travelerId, service.CreateRequest!.RequestedByEmployeeId);
    }

    [Fact]
    public async Task DeleteTrip_ReadsRequesterClaimAndReturnsNoContent()
    {
        var requesterId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var service = new RecordingTripService();
        var controller = CreateControllerWithEmployeeClaim(service, requesterId);

        var result = await controller.DeleteTrip(tripId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(requesterId, service.DeleteRequesterEmployeeId);
        Assert.Equal(tripId, service.DeleteTripId);
    }

    [Fact]
    public async Task GetTrips_WhenEmployeeClaimIsMissing_Returns401WithStructuredPayload()
    {
        var controller = new TripsController(new RecordingTripService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var result = await controller.GetTrips(null, 1, 25, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        AssertErrorPayload(unauthorized.Value, "UNAUTHORIZED");
    }

    [Fact]
    public async Task GetTrip_WhenServiceReturnsForbidden_Returns403WithStructuredPayload()
    {
        var controller = CreateControllerWithEmployeeClaim(
            new ErrorTripService(ServiceError.Forbidden("Forbidden trip.")),
            Guid.NewGuid());

        var result = await controller.GetTrip(Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        AssertErrorPayload(forbidden.Value, "FORBIDDEN");
    }

    [Fact]
    public async Task GetTrip_WhenServiceReturnsNotFound_Returns404WithStructuredPayload()
    {
        var controller = CreateControllerWithEmployeeClaim(
            new ErrorTripService(ServiceError.NotFound("Trip was not found.")),
            Guid.NewGuid());

        var result = await controller.GetTrip(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        AssertErrorPayload(notFound.Value, "NOT_FOUND");
    }

    [Fact]
    public async Task CreateTrip_WhenServiceReturnsBusinessRule_Returns422WithStructuredPayload()
    {
        var travelerId = Guid.NewGuid();
        var controller = CreateControllerWithEmployeeClaim(
            new ErrorTripService(ServiceError.BusinessRule("Trip business rule failed.")),
            Guid.NewGuid());

        var result = await controller.CreateTrip(BuildRequest(travelerId), CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
        AssertErrorPayload(unprocessable.Value, "BUSINESS_RULE_VIOLATION");
    }

    private static TripsController CreateControllerWithEmployeeClaim(
        ITripService service,
        Guid employeeId)
    {
        return new TripsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", employeeId.ToString())
                    ], "test"))
                }
            }
        };
    }

    private static TripCreateRequest BuildRequest(Guid travelerId)
    {
        return new TripCreateRequest
        {
            RequestedByEmployeeId = travelerId,
            ReferenceName = "Site Visit",
            Project = "HR",
            Route = "HQ to Client",
            TripType = "Business",
            TripDate = new DateOnly(2026, 6, 10)
        };
    }

    private static void AssertErrorPayload(object? payload, string expectedCode)
    {
        Assert.NotNull(payload);
        var json = JsonSerializer.Serialize(payload);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("message").GetString()));
    }

    private sealed class RecordingTripService : ITripService
    {
        public Guid GetTripsRequesterEmployeeId { get; private set; }
        public Guid? GetTripsTravelerEmployeeId { get; private set; }
        public int GetTripsPage { get; private set; }
        public int GetTripsPageSize { get; private set; }
        public Guid CreateRequesterEmployeeId { get; private set; }
        public TripCreateRequest? CreateRequest { get; private set; }
        public Guid DeleteRequesterEmployeeId { get; private set; }
        public Guid DeleteTripId { get; private set; }

        public Task<Result<PagedList<TripResponse>>> GetTripsAsync(
            Guid requesterEmployeeId,
            Guid? travelerEmployeeId,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            GetTripsRequesterEmployeeId = requesterEmployeeId;
            GetTripsTravelerEmployeeId = travelerEmployeeId;
            GetTripsPage = page;
            GetTripsPageSize = pageSize;

            return Task.FromResult(Result<PagedList<TripResponse>>.Success(
                new PagedList<TripResponse>([], 0, page, pageSize)));
        }

        public Task<Result<TripResponse>> GetTripByIdAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
            => Task.FromResult(Result<TripResponse>.Success(new TripResponse { Id = id }));

        public Task<Result<TripResponse>> CreateTripAsync(
            Guid requesterEmployeeId,
            TripCreateRequest request,
            CancellationToken ct)
        {
            CreateRequesterEmployeeId = requesterEmployeeId;
            CreateRequest = request;

            return Task.FromResult(Result<TripResponse>.Success(new TripResponse
            {
                Id = Guid.NewGuid(),
                RequestedByEmployeeId = request.RequestedByEmployeeId
            }));
        }

        public Task<Result> DeleteTripAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
        {
            DeleteRequesterEmployeeId = requesterEmployeeId;
            DeleteTripId = id;

            return Task.FromResult(Result.Success());
        }
    }

    private sealed class ErrorTripService(ServiceError error) : ITripService
    {
        public Task<Result<PagedList<TripResponse>>> GetTripsAsync(
            Guid requesterEmployeeId,
            Guid? travelerEmployeeId,
            int page,
            int pageSize,
            CancellationToken ct)
            => Task.FromResult(Result<PagedList<TripResponse>>.Failure(error));

        public Task<Result<TripResponse>> GetTripByIdAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
            => Task.FromResult(Result<TripResponse>.Failure(error));

        public Task<Result<TripResponse>> CreateTripAsync(
            Guid requesterEmployeeId,
            TripCreateRequest request,
            CancellationToken ct)
            => Task.FromResult(Result<TripResponse>.Failure(error));

        public Task<Result> DeleteTripAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
            => Task.FromResult(Result.Failure(error));
    }
}
