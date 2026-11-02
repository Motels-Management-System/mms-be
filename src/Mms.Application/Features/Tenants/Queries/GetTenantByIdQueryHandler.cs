using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Dtos;

namespace Mms.Application.Features.Tenants.Queries;

public sealed class GetTenantByIdQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetTenantByIdQuery> validator) : IQueryHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<TenantDto> HandleAsync(GetTenantByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == query.Id)
            .Select(t => new TenantDto(t.Id, t.FullName, t.Phone, t.UserId, t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Hồ sơ người thuê", query.Id);
    }
}
