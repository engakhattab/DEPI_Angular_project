using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.DTOs.VacationRequests;
using HR.Application.VacationRequests;
using HR.Domain.Enums;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.VacationRequests;

public class VacationRequestsControllerTests
{
    [Fact]
    public async Task UpdateVacationStatus_ReadsReviewerIdFromEmployeeClaim()
    {
        var requestId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var service = new RecordingVacationRequestService();
        var controller = new VacationRequestsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", reviewerId.ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.UpdateVacationStatus(
            requestId,
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(requestId, service.CapturedRequestId);
        Assert.Equal(reviewerId, service.CapturedReviewerId);
    }

    [Fact]
    public async Task UpdateVacationStatus_WhenReviewerClaimIsMissingOrInvalid_ReturnsUnauthorized()
    {
        var controller = new VacationRequestsController(new RecordingVacationRequestService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", "not-a-guid")
                    ], "test"))
                }
            }
        };

        var result = await controller.UpdateVacationStatus(
            Guid.NewGuid(),
            new VacationRequestStatusUpdateRequest { Status = VacationRequestStatus.Approved },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetVacationRequests_WhenEmployeeClaimIsMissing_ReturnsUnauthorized()
    {
        var controller = new VacationRequestsController(new RecordingVacationRequestService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var result = await controller.GetVacationRequests(
            null, null, 1, 25, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetVacationRequest_WhenEmployeeClaimIsMissing_ReturnsUnauthorized()
    {
        var controller = new VacationRequestsController(new RecordingVacationRequestService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var result = await controller.GetVacationRequest(
            Guid.NewGuid(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task CreateVacationRequest_WhenEmployeeClaimIsMissing_ReturnsUnauthorized()
    {
        var controller = new VacationRequestsController(new RecordingVacationRequestService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var result = await controller.CreateVacationRequest(
            new VacationRequestCreateRequest
            {
                EmployeeId = Guid.NewGuid(),
                StartDate = new DateOnly(2026, 7, 1),
                EndDate = new DateOnly(2026, 7, 3),
                Reason = "Test"
            },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task DeleteVacationRequest_WhenEmployeeClaimIsMissing_ReturnsUnauthorized()
    {
        var controller = new VacationRequestsController(new RecordingVacationRequestService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var result = await controller.DeleteVacationRequest(
            Guid.NewGuid(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetVacationRequests_WhenServiceReturnsForbidden_Returns403WithCodeMessage()
    {
        var service = new ErrorResultService(ServiceError.Forbidden());
        var controller = new VacationRequestsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("employee_id", Guid.NewGuid().ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.GetVacationRequests(
            null, null, 1, 25, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
        AssertErrorPayload(statusCodeResult.Value, "FORBIDDEN");
    }

    [Fact]
    public async Task GetVacationRequest_WhenServiceReturnsNotFound_Returns404WithCodeMessage()
    {
        var service = new ErrorResultService(ServiceError.NotFound("Vacation request was not found."));
        var controller = CreateControllerWithEmployeeClaim(service, Guid.NewGuid());

        var result = await controller.GetVacationRequest(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        AssertErrorPayload(notFound.Value, "NOT_FOUND");
    }

    [Fact]
    public async Task CreateVacationRequest_WhenServiceReturnsBusinessRule_Returns422WithCodeMessage()
    {
        var service = new ErrorResultService(ServiceError.BusinessRule("Business rule failed."));
        var employeeId = Guid.NewGuid();
        var controller = CreateControllerWithEmployeeClaim(service, employeeId);

        var result = await controller.CreateVacationRequest(
            new VacationRequestCreateRequest
            {
                EmployeeId = employeeId,
                StartDate = new DateOnly(2026, 7, 1),
                EndDate = new DateOnly(2026, 7, 3),
                Reason = "Test"
            },
            CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
        AssertErrorPayload(unprocessable.Value, "BUSINESS_RULE_VIOLATION");
    }

    private static VacationRequestsController CreateControllerWithEmployeeClaim(
        IVacationRequestService service,
        Guid employeeId)
    {
        return new VacationRequestsController(service)
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

    private static void AssertErrorPayload(object? payload, string expectedCode)
    {
        Assert.NotNull(payload);
        var json = JsonSerializer.Serialize(payload);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("message").GetString()));
    }

    private sealed class ErrorResultService(ServiceError error) : IVacationRequestService
    {
        public Task<Result<PagedList<VacationRequestResponse>>> GetVacationRequestsAsync(
            Guid requesterEmployeeId, VacationRequestStatus? status, Guid? employeeId, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(Result<PagedList<VacationRequestResponse>>.Failure(error));

        public Task<Result<VacationRequestResponse>> GetVacationRequestByIdAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct)
            => Task.FromResult(Result<VacationRequestResponse>.Failure(error));

        public Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(Guid requesterEmployeeId, VacationRequestCreateRequest request, CancellationToken ct)
            => Task.FromResult(Result<VacationRequestResponse>.Failure(error));

        public Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(Guid id, Guid reviewerEmployeeId, VacationRequestStatusUpdateRequest request, CancellationToken ct)
            => Task.FromResult(Result<VacationRequestResponse>.Failure(error));

        public Task<Result> DeleteVacationRequestAsync(Guid requesterEmployeeId, Guid id, CancellationToken ct)
            => Task.FromResult(Result.Failure(error));
    }

    private sealed class RecordingVacationRequestService : IVacationRequestService
    {
        public Guid CapturedRequestId { get; private set; }

        public Guid CapturedReviewerId { get; private set; }

        public Task<Result<PagedList<VacationRequestResponse>>> GetVacationRequestsAsync(
            Guid requesterEmployeeId,
            VacationRequestStatus? status,
            Guid? employeeId,
            int page,
            int pageSize,
            CancellationToken ct)
            => Task.FromResult(Result<PagedList<VacationRequestResponse>>.Success(
                new PagedList<VacationRequestResponse>([], 0, 1, 25)));

        public Task<Result<VacationRequestResponse>> GetVacationRequestByIdAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
            => Task.FromResult(Result<VacationRequestResponse>.Failure(
                ServiceError.NotFound("Not found")));

        public Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(
            Guid requesterEmployeeId,
            VacationRequestCreateRequest request,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<VacationRequestResponse>> UpdateVacationStatusAsync(
            Guid id,
            Guid reviewerEmployeeId,
            VacationRequestStatusUpdateRequest request,
            CancellationToken ct)
        {
            CapturedRequestId = id;
            CapturedReviewerId = reviewerEmployeeId;

            return Task.FromResult(Result<VacationRequestResponse>.Success(new VacationRequestResponse
            {
                Id = id,
                EmployeeId = Guid.NewGuid(),
                Status = request.Status
            }));
        }

        public Task<Result> DeleteVacationRequestAsync(
            Guid requesterEmployeeId,
            Guid id,
            CancellationToken ct)
            => throw new NotSupportedException();
    }
}
