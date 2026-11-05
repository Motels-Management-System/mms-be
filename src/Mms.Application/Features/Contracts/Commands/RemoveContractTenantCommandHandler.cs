using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class RemoveContractTenantCommandHandler(
    IAppDbContext dbContext,
    IValidator<RemoveContractTenantCommand> validator) : ICommandHandler<RemoveContractTenantCommand>
{
    public async Task HandleAsync(RemoveContractTenantCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        contract.RemoveTenant(command.TenantId);
        await dbContext.SaveChangesAsync(ct);
    }
}
