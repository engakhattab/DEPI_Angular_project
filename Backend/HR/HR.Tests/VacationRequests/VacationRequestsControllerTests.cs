using System.Security.Claims;
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

        Assert.IsType<OkObjectResult>(result.Result);
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

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    private sealed class RecordingVacationRequestService : IVacationRequestService
    {
        public Guid CapturedRequestId { get; private set; }

        public Guid CapturedReviewerId { get; private set; }

        public Task<PagedList<VacationRequestResponse>> GetVacationRequestsAsync(
            VacationRequestStatus? status,
            Guid? employeeId,
            int page,
            int pageSize,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<VacationRequestResponse?> GetVacationRequestByIdAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<VacationRequestResponse>> CreateVacationRequestAsync(VacationRequestCreateRequest request, CancellationToken ct)
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

        public Task<Result> DeleteVacationRequestAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
