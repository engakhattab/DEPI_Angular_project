using System.Text.Json;
using HR.Application.Audit;
using HR.Application.Authorization;
using HR.Application.DTOs.Audit;
using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Repositories;
using HR.Shared.Pagination;
using HR.Shared.Results;

namespace HR.Infrastructure.Audit;

public class AuditLogService(
    IAuditLogRepository auditLogRepository,
    IEmployeeAccessService accessService) : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository = auditLogRepository;
    private readonly IEmployeeAccessService _accessService = accessService;

    public async Task<Result<PagedList<AuditLogEntryResponse>>> SearchAsync(Guid requesterEmployeeId, AuditLogQueryRequest request, CancellationToken ct)
    {
        if (!await _accessService.HasAnyRoleAsync(requesterEmployeeId, ct, EmployeeRole.HRAdministrator, EmployeeRole.SystemAdministrator))
        {
            return Result<PagedList<AuditLogEntryResponse>>.Failure(ServiceError.Forbidden());
        }

        AuditActionType? action = null;
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            if (!Enum.TryParse<AuditActionType>(request.Action, true, out var parsed))
            {
                return Result<PagedList<AuditLogEntryResponse>>.Failure(ServiceError.Validation("Audit action filter is invalid."));
            }

            action = parsed;
        }

        var page = await _auditLogRepository.SearchAsync(
            request.EntityType,
            request.EntityId,
            request.ActorEmployeeId,
            action,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            ct);

        return Result<PagedList<AuditLogEntryResponse>>.Success(
            new PagedList<AuditLogEntryResponse>(page.Items.Select(Map).ToList(), page.TotalCount, page.Page, page.PageSize));
    }

    private static AuditLogEntryResponse Map(AuditLogEntry entry)
    {
        return new AuditLogEntryResponse
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            ActionType = entry.ActionType,
            ActorEmployeeId = entry.ActorEmployeeId,
            ActorMarker = entry.ActorMarker,
            PerformedAt = entry.PerformedAt,
            ChangedFields = Deserialize<string[]>(entry.ChangedFields) ?? [],
            OldValues = Deserialize<object>(entry.OldValues),
            NewValues = Deserialize<object>(entry.NewValues),
            SensitiveSummary = entry.SensitiveSummary
        };
    }

    private static T? Deserialize<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value);
    }
}
