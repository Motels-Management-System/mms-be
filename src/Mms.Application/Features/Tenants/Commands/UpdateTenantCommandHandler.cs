using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Dtos;

namespace Mms.Application.Features.Tenants.Commands;

public sealed class UpdateTenantCommandHandler(
    IAppDbContext dbContext,
    IValidator<UpdateTenantCommand> validator) : ICommandHandler<UpdateTenantCommand, TenantDto>
{
    public async Task<TenantDto> HandleAsync(UpdateTenantCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Hồ sơ người thuê", command.Id);

        tenant.UpdateProfile(command.FullName, command.Phone);
        await dbContext.SaveChangesAsync(ct);

        return new TenantDto(
            tenant.Id,
            tenant.FullName,
            tenant.Phone,
            tenant.UserId,
            tenant.CreatedAt,
            tenant.UpdatedAt);
    }
}
