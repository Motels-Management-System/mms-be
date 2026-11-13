using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.AuditLogs.Dtos;

namespace Mms.Application.Features.AuditLogs.Queries;

public sealed class GetAuditLogsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetAuditLogsQuery> validator) : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> HandleAsync(GetAuditLogsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            var entityName = query.EntityName.Trim();
            baseQuery = baseQuery.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = Guid.Parse(query.EntityId).ToString();
            baseQuery = baseQuery.Where(a => a.EntityId == entityId);
        }

        if (query.UserId.HasValue)
        {
            var userId = query.UserId.Value;
            baseQuery = baseQuery.Where(a => a.UserId == userId);
        }

        if (query.From.HasValue)
        {
            var from = ToUtc(query.From.Value.ToDateTime(TimeOnly.MinValue));
            baseQuery = baseQuery.Where(a => a.CreatedAt >= from);
        }

        if (query.To.HasValue)
        {
            // To là ngày cuối của khoảng nên mốc so sánh là 00:00 UTC của ngày kế tiếp.
            var toExclusive = ToUtc(query.To.Value.ToDateTime(TimeOnly.MinValue).AddDays(1));
            baseQuery = baseQuery.Where(a => a.CreatedAt < toExclusive);
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.OldValues,
                a.NewValues,
                a.CreatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(a => new AuditLogDto(
            a.Id,
            a.UserId,
            a.EntityName,
            a.EntityId,
            a.Action,
            ParseJson(a.OldValues),
            ParseJson(a.NewValues),
            a.CreatedAt)).ToList();

        return new PagedResult<AuditLogDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static JsonElement? ParseJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
