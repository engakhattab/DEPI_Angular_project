using System.Security.Claims;
using System.Text.Json;
using HR.API.Controllers;
using HR.Application.Attendance;
using HR.Application.DTOs.Attendance;
using HR.Shared.Pagination;
using HR.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Attendance;

public class AttendanceControllerTests
{
    [Fact]
    public async Task ClockIn_ReadsEmployeeClaimAndReturnsCreated()
    {
        var employeeId = Guid.NewGuid();
        var service = new RecordingAttendanceService
        {
            ClockInResult = Result<AttendanceRecordResponse>.Success(new AttendanceRecordResponse
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                AttendanceDate = new DateOnly(2026, 6, 7),
                ClockInAtUtc = new DateTimeOffset(2026, 6, 7, 6, 0, 0, TimeSpan.Zero)
            })
        };
        var controller = CreateController(service, employeeId);

        var result = await controller.ClockIn(new AttendanceClockInRequest { Notes = "Start" }, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var payload = Assert.IsType<AttendanceRecordResponse>(created.Value);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.Equal(employeeId, service.ClockInEmployeeId);
        Assert.Equal("Start", service.ClockInRequest!.Notes);
    }

    [Fact]
    public async Task ClockOut_ReadsEmployeeClaimAndReturnsOk()
    {
        var employeeId = Guid.NewGuid();
        var service = new RecordingAttendanceService
        {
            ClockOutResult = Result<AttendanceRecordResponse>.Success(new AttendanceRecordResponse
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                AttendanceDate = new DateOnly(2026, 6, 7),
                ClockInAtUtc = new DateTimeOffset(2026, 6, 7, 6, 0, 0, TimeSpan.Zero),
                ClockOutAtUtc = new DateTimeOffset(2026, 6, 7, 14, 0, 0, TimeSpan.Zero)
            })
        };
        var controller = CreateController(service, employeeId);

        var result = await controller.ClockOut(new AttendanceClockOutRequest { Notes = "End" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<AttendanceRecordResponse>(ok.Value);
        Assert.Equal(employeeId, service.ClockOutEmployeeId);
        Assert.Equal("End", service.ClockOutRequest!.Notes);
    }

    [Fact]
    public async Task GetAttendance_ReadsRequesterClaimAndPassesQuery()
    {
        var requesterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var service = new RecordingAttendanceService
        {
            GetAttendanceResult = Result<PagedList<AttendanceRecordResponse>>.Success(
                new PagedList<AttendanceRecordResponse>([], 0, 2, 10))
        };
        var controller = CreateController(service, requesterId);

        var result = await controller.GetAttendance(
            employeeId: targetId,
            from: new DateOnly(2026, 6, 1),
            to: new DateOnly(2026, 6, 30),
            page: 2,
            pageSize: 10,
            ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedList<AttendanceRecordResponse>>(ok.Value);
        Assert.Equal(requesterId, service.GetAttendanceRequesterId);
        Assert.Equal(targetId, service.QueryRequest!.EmployeeId);
        Assert.Equal(new DateOnly(2026, 6, 1), service.QueryRequest.From);
        Assert.Equal(new DateOnly(2026, 6, 30), service.QueryRequest.To);
        Assert.Equal(2, service.QueryRequest.Page);
        Assert.Equal(10, service.QueryRequest.PageSize);
    }

    [Theory]
    [InlineData("clock-in")]
    [InlineData("clock-out")]
    [InlineData("get")]
    public async Task AttendanceEndpoints_WhenEmployeeClaimIsMissing_ReturnUnauthorizedStructuredPayload(string endpoint)
    {
        var controller = new AttendanceController(new RecordingAttendanceService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        object? actionResult = endpoint switch
        {
            "clock-in" => (await controller.ClockIn(new AttendanceClockInRequest(), CancellationToken.None)).Result,
            "clock-out" => (await controller.ClockOut(new AttendanceClockOutRequest(), CancellationToken.None)).Result,
            _ => (await controller.GetAttendance(ct: CancellationToken.None)).Result
        };

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unauthorized.Value));
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid session.", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ClockIn_WhenDuplicateConflictOccurs_ReturnsStructuredConflictPayload()
    {
        var service = new RecordingAttendanceService
        {
            ClockInResult = Result<AttendanceRecordResponse>.Failure(
                ServiceError.Conflict("Attendance has already been recorded for this business date."))
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.ClockIn(new AttendanceClockInRequest(), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(conflict.Value));
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal("CONFLICT", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ClockOut_WhenBusinessRuleFails_ReturnsStructuredUnprocessableEntityPayload()
    {
        var service = new RecordingAttendanceService
        {
            ClockOutResult = Result<AttendanceRecordResponse>.Failure(
                ServiceError.BusinessRule("Clock-out must be after clock-in."))
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.ClockOut(new AttendanceClockOutRequest(), CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(unprocessable.Value));
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
        Assert.Equal("BUSINESS_RULE_VIOLATION", payload.RootElement.GetProperty("code").GetString());
    }

    private static AttendanceController CreateController(IAttendanceService service, Guid employeeId)
    {
        return new AttendanceController(service)
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

    private sealed class RecordingAttendanceService : IAttendanceService
    {
        public Guid ClockInEmployeeId { get; private set; }
        public AttendanceClockInRequest? ClockInRequest { get; private set; }
        public Result<AttendanceRecordResponse>? ClockInResult { get; init; }
        public Guid ClockOutEmployeeId { get; private set; }
        public AttendanceClockOutRequest? ClockOutRequest { get; private set; }
        public Result<AttendanceRecordResponse>? ClockOutResult { get; init; }
        public Guid GetAttendanceRequesterId { get; private set; }
        public AttendanceQueryRequest? QueryRequest { get; private set; }
        public Result<PagedList<AttendanceRecordResponse>>? GetAttendanceResult { get; init; }

        public Task<Result<AttendanceRecordResponse>> ClockInAsync(Guid employeeId, AttendanceClockInRequest request, CancellationToken ct)
        {
            ClockInEmployeeId = employeeId;
            ClockInRequest = request;
            return Task.FromResult(ClockInResult ?? throw new NotSupportedException());
        }

        public Task<Result<AttendanceRecordResponse>> ClockOutAsync(Guid employeeId, AttendanceClockOutRequest request, CancellationToken ct)
        {
            ClockOutEmployeeId = employeeId;
            ClockOutRequest = request;
            return Task.FromResult(ClockOutResult ?? throw new NotSupportedException());
        }

        public Task<Result<PagedList<AttendanceRecordResponse>>> GetAttendanceAsync(Guid requesterEmployeeId, AttendanceQueryRequest request, CancellationToken ct)
        {
            GetAttendanceRequesterId = requesterEmployeeId;
            QueryRequest = request;
            return Task.FromResult(GetAttendanceResult ?? throw new NotSupportedException());
        }
    }
}
