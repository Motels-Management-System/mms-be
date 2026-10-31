using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Auth.Dtos;

namespace Mms.Application.Features.Auth.Queries;

public sealed class GetMeQueryHandler(
    IAppDbContext dbContext,
    IIdentityService identityService,
    IValidator<GetMeQuery> validator) : IQueryHandler<GetMeQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> HandleAsync(GetMeQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var user = await identityService.GetUserByIdAsync(query.UserId);
        if (user is null)
            throw new NotFoundException("Người dùng", query.UserId);

        TenantProfileDto? tenantProfile = null;
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == query.UserId, ct);

        if (tenant is not null)
        {
            tenantProfile = new TenantProfileDto(tenant.Id, tenant.FullName, tenant.Phone);
        }

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.IsActive,
            user.Roles,
            tenantProfile);
    }
}
