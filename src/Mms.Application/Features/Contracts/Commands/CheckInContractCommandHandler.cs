using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class CheckInContractCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<CheckInContractCommand> validator) : ICommandHandler<CheckInContractCommand>
{
    public async Task HandleAsync(CheckInContractCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == contract.RoomId, ct)
            ?? throw new NotFoundException("Phòng", contract.RoomId);

        if (clock.Today < contract.StartDate)
            throw new DomainException($"Chưa đến ngày nhận phòng ({contract.StartDate:yyyy-MM-dd}).");

        if (room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
            throw new DomainException($"Không thể nhận phòng khi phòng đang {room.Status}.");

        contract.Activate();
        room.ChangeStatus(RoomStatus.Occupied);

        dbContext.HandoverRecords.Add(new HandoverRecord(
            contract.Id, HandoverType.CheckIn, command.RoomCondition, command.KeyCount,
            command.StaffUserId, command.Notes));

        await dbContext.SaveChangesAsync(ct);
    }
}
