using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.Compensation;
using HR.Application.DTOs.Compensation;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Compensation;

public class CompensationControllerTests
{
    [Fact]
    public async Task Get_ReadsRequesterClaimAndReturnsCompensation()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var service = new RecordingCompensationService
        {
            GetResult = Result<CompensationResponse>.Success(new CompensationResponse
            {
                EmployeeId = employeeId,
                BaseSalary = 10000m,
                SalaryCurrency = "EGP"
            })
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.Get(employeeId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CompensationResponse>(ok.Value);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.Equal(requesterId, service.GetRequesterId);
        Assert.Equal(employeeId, service.GetEmployeeId);
    }

    [Fact]
    public async Task Update_ReadsRequesterClaimAndReturnsCompensation()
    {
        var requesterId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var service = new RecordingCompensationService
        {
            UpdateResult = Result<CompensationResponse>.Success(new CompensationResponse
            {
                EmployeeId = employeeId,
                BaseSalary = 12000m,
                SalaryCurrency = "USD"
            })
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.Update(
            employeeId,
            new CompensationUpdateRequest { BaseSalary = 12000m, SalaryCurrency = "usd" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CompensationResponse>(ok.Value);
        Assert.Equal(requesterId, service.UpdateRequesterId);
        Assert.Equal(employeeId, service.UpdateEmployeeId);
        Assert.Equal(12000m, service.UpdateRequest!.BaseSalary);
        Assert.Equal("usd", service.UpdateRequest.SalaryCurrency);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("update")]
    public async Task CompensationEndpoints_WhenEmployeeClaimIsMissing_ReturnUnauthorizedStructuredPayload(string endpoint)
    {
        var controller = new CompensationController(new RecordingCompensationService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        object? actionResult = endpoint == "get"
            ? (await controller.Get(Guid.NewGuid(), CancellationToken.None)).Result
            : (await controller.Update(Guid.NewGuid(), new CompensationUpdateRequest(), CancellationToken.None)).Result;

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Update_WhenBusinessRuleFails_ReturnsStructuredUnprocessableEntityPayload()
    {
        var service = new RecordingCompensationService
        {
            UpdateResult = Result<CompensationResponse>.Failure(ServiceError.BusinessRule("Base salary must be non-negative."))
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Update(Guid.NewGuid(), new CompensationUpdateRequest { BaseSalary = -1m }, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unprocessable.Value));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
        Assert.Equal("BUSINESS_RULE_VIOLATION", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_WhenForbidden_ReturnsStructuredForbiddenPayload()
    {
        var service = new RecordingCompensationService
        {
            GetResult = Result<CompensationResponse>.Failure(ServiceError.Forbidden())
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(forbidden.Value));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("code").GetString());
    }

    private static CompensationController CreateController(ICompensationService service, Guid employeeId)
    {
        return new CompensationController(service)
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

    private sealed class RecordingCompensationService : ICompensationService
    {
        public Guid GetRequesterId { get; private set; }
        public Guid GetEmployeeId { get; private set; }
        public Result<CompensationResponse>? GetResult { get; init; }
        public Guid UpdateRequesterId { get; private set; }
        public Guid UpdateEmployeeId { get; private set; }
        public CompensationUpdateRequest? UpdateRequest { get; private set; }
        public Result<CompensationResponse>? UpdateResult { get; init; }

        public Task<Result<CompensationResponse>> GetAsync(Guid requesterEmployeeId, Guid employeeId, CancellationToken ct)
        {
            GetRequesterId = requesterEmployeeId;
            GetEmployeeId = employeeId;
            return Task.FromResult(GetResult ?? throw new NotSupportedException());
        }

        public Task<Result<CompensationResponse>> UpdateAsync(Guid requesterEmployeeId, Guid employeeId, CompensationUpdateRequest request, CancellationToken ct)
        {
            UpdateRequesterId = requesterEmployeeId;
            UpdateEmployeeId = employeeId;
            UpdateRequest = request;
            return Task.FromResult(UpdateResult ?? throw new NotSupportedException());
        }
    }
}
