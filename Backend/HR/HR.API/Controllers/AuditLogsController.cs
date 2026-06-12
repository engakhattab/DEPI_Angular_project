using HR.API.Extensions;
using HR.Application.Audit;
using HR.Application.DTOs.Audit;
using HR.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[ApiController]
[Authorize(Policy = "HRAdministrator")]
[Route("api/audit-logs")]
public class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    private readonly IAuditLogService _auditLogService = auditLogService;

    [HttpGet]
    public async Task<ActionResult<PagedList<AuditLogEntryResponse>>> Search(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? actorEmployeeId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _auditLogService.SearchAsync(
            requesterId.Value,
            new AuditLogQueryRequest
            {
                EntityType = entityType,
                EntityId = entityId,
                ActorEmployeeId = actorEmployeeId,
                Action = action,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
