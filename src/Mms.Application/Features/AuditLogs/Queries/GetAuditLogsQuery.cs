namespace Mms.Application.Features.AuditLogs.Queries;

/// <summary>
/// Tra cứu nhật ký thay đổi. <paramref name="From"/> và <paramref name="To"/> là ngày,
/// so sánh theo mốc UTC (To bao gồm cả ngày).
/// </summary>
public sealed record GetAuditLogsQuery(
    string? EntityName = null,
    string? EntityId = null,
    Guid? UserId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20);
