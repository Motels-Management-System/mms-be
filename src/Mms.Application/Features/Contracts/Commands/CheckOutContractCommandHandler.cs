using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class CheckOutContractCommandHandler(
    IAppDbContext dbContext,
    IValidator<CheckOutContractCommand> validator) : ICommandHandler<CheckOutContractCommand>
{
    public async Task HandleAsync(CheckOutContractCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        if (contract.Status is not (ContractStatus.Expired or ContractStatus.Terminated))
            throw new DomainException("Chỉ trả phòng được khi hợp đồng đã hết hạn hoặc bị chấm dứt.");

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == contract.RoomId, ct)
            ?? throw new NotFoundException("Phòng", contract.RoomId);

        if (room.Status != RoomStatus.Occupied)
            throw new DomainException($"Không thể trả phòng khi phòng đang {room.Status}.");

        room.ChangeStatus(command.ToMaintenance ? RoomStatus.Maintenance : RoomStatus.Available);

        dbContext.HandoverRecords.Add(new HandoverRecord(
            contract.Id, HandoverType.CheckOut, command.RoomCondition, command.KeyCount,
            command.StaffUserId, command.Notes));

        await dbContext.SaveChangesAsync(ct);
    }
}
