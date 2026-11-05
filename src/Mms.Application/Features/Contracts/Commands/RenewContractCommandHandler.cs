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

public sealed class RenewContractCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IOptions<RentalOptions> rentalOptions,
    IValidator<RenewContractCommand> validator) : ICommandHandler<RenewContractCommand, ContractDetailDto>
{
    public async Task<ContractDetailDto> HandleAsync(RenewContractCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        if (command.Months < rentalOptions.Value.MinContractMonths)
            throw new DomainException($"Thời hạn hợp đồng tối thiểu là {rentalOptions.Value.MinContractMonths} tháng.");

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        var renewal = contract.CreateRenewal(command.Months);
        dbContext.Contracts.Add(renewal);

        // Hợp đồng mới nối tiếp đúng EndDate nên không chồng lấn; nếu có race
        // thì exclusion constraint trả 23P01 → 409 ROOM_DATE_OVERLAP.
        await dbContext.SaveChangesAsync(ct);

        var roomCode = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == renewal.RoomId)
            .Select(r => r.Code)
            .FirstAsync(ct);

        var tenantIds = renewal.Tenants.Select(t => t.TenantId).ToList();
        var tenantLookup = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => (t.FullName, t.Phone), ct);

        return ContractDtoMapper.ToDetail(renewal, roomCode, clock, tenantLookup: tenantLookup);
    }
}
