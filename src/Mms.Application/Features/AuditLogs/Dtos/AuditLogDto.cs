namespace Mms.Application.Features.AuditLogs.Dtos;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string EntityName,
    string EntityId,
    string Action,
    System.Text.Json.JsonElement? OldValues,
    System.Text.Json.JsonElement? NewValues,
    DateTimeOffset CreatedAt);
