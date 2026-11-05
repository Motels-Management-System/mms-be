using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class AddContractTenantCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<AddContractTenantCommand> validator) : ICommandHandler<AddContractTenantCommand, ContractDetailDto>
{
    public async Task<ContractDetailDto> HandleAsync(AddContractTenantCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        var maxOccupancy = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == contract.RoomId)
            .Select(r => r.RoomType.MaxOccupancy)
            .FirstAsync(ct);

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, ct)
            ?? throw new NotFoundException("Người thuê", command.TenantId);

        contract.AddTenant(command.TenantId, command.IsPrimary, maxOccupancy);
        await dbContext.SaveChangesAsync(ct);

        var roomCode = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == contract.RoomId)
            .Select(r => r.Code)
            .FirstAsync(ct);

        var handovers = await dbContext.HandoverRecords
            .AsNoTracking()
            .Where(h => h.ContractId == contract.Id)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(ct);

        var tenantIds = contract.Tenants.Select(t => t.TenantId).ToList();
        var tenantLookup = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => (t.FullName, t.Phone), ct);

        return ContractDtoMapper.ToDetail(contract, roomCode, clock, handovers, tenantLookup);
    }
}
