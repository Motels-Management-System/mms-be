using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Dtos;

namespace Mms.Application.Features.Tenants.Queries;

public sealed class GetTenantsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetTenantsQuery> validator) : IQueryHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> HandleAsync(GetTenantsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.Tenants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLower();
            baseQuery = baseQuery.Where(t =>
                t.FullName.ToLower().Contains(keyword) || t.Phone.ToLower().Contains(keyword));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(t => t.FullName)
            .ThenBy(t => t.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TenantDto(t.Id, t.FullName, t.Phone, t.UserId, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TenantDto>(items, query.Page, query.PageSize, totalCount);
    }
}
