using HR.API.Extensions;
using HR.Application.Compensation;
using HR.Application.DTOs.Compensation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Controllers;

[ApiController]
[Authorize(Policy = "HRAdministrator")]
[Route("api/employees/{employeeId:guid}/compensation")]
public class CompensationController(ICompensationService compensationService) : ControllerBase
{
    private readonly ICompensationService _compensationService = compensationService;

    [HttpGet]
    public async Task<ActionResult<CompensationResponse>> Get(Guid employeeId, CancellationToken ct)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _compensationService.GetAsync(requesterId.Value, employeeId, ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }

    [HttpPut]
    public async Task<ActionResult<CompensationResponse>> Update(Guid employeeId, [FromBody] CompensationUpdateRequest request, CancellationToken ct)
    {
        var requesterId = User.GetEmployeeId();
        if (!requesterId.HasValue)
        {
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid session." });
        }

        var result = await _compensationService.UpdateAsync(requesterId.Value, employeeId, request, ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
