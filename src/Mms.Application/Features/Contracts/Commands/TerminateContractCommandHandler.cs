using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class TerminateContractCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IOptions<RentalOptions> rentalOptions,
    IValidator<TerminateContractCommand> validator) : ICommandHandler<TerminateContractCommand, ContractDetailDto>
{
    public async Task<ContractDetailDto> HandleAsync(TerminateContractCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        var earliest = clock.Today.AddDays(rentalOptions.Value.EarlyTerminationNoticeDays);
        if (command.TerminationDate < earliest)
            throw new DomainException(
                $"Chấm dứt sớm cần báo trước ít nhất {rentalOptions.Value.EarlyTerminationNoticeDays} ngày " +
                $"(sớm nhất là {earliest:yyyy-MM-dd}).");

        contract.Terminate(command.TerminationDate);
        await dbContext.SaveChangesAsync(ct);

        var roomCode = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == contract.RoomId)
            .Select(r => r.Code)
            .FirstAsync(ct);

        var tenantIds = contract.Tenants.Select(t => t.TenantId).ToList();
        var tenantLookup = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => (t.FullName, t.Phone), ct);

        var handovers = await dbContext.HandoverRecords
            .AsNoTracking()
            .Where(h => h.ContractId == contract.Id)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(ct);

        return ContractDtoMapper.ToDetail(contract, roomCode, clock, handovers, tenantLookup);
    }
}
