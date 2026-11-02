using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Features.Tenants.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Tenants.Commands;

public sealed class CreateTenantCommandHandler(
    IAppDbContext dbContext,
    IValidator<CreateTenantCommand> validator) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> HandleAsync(CreateTenantCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var tenant = new Tenant(command.FullName, command.Phone);

        dbContext.Tenants.Add(tenant);
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
