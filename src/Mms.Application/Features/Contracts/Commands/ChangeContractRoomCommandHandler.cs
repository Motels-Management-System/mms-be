using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class ChangeContractRoomCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<ChangeContractRoomCommand> validator) : ICommandHandler<ChangeContractRoomCommand>
{
    public async Task HandleAsync(ChangeContractRoomCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var contract = await dbContext.Contracts
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == command.ContractId, ct)
            ?? throw new NotFoundException("Hợp đồng", command.ContractId);

        var oldRoom = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == contract.RoomId, ct)
            ?? throw new NotFoundException("Phòng", contract.RoomId);

        var newRoom = await dbContext.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == command.NewRoomId, ct)
            ?? throw new NotFoundException("Phòng", command.NewRoomId);

        if (newRoom.Status != RoomStatus.Available)
            throw new DomainException($"Phòng mới phải đang trống, hiện đang {newRoom.Status}.");

        if (newRoom.RoomType.MaxOccupancy < contract.Tenants.Count)
            throw new DomainException($"Sức chứa phòng mới chỉ {newRoom.RoomType.MaxOccupancy} người.");

        // Phòng mới phải trống trong toàn bộ thời gian còn lại [today, EndDate).
        var today = clock.Today;
        var overlaps = await dbContext.Contracts.AsNoTracking().AnyAsync(
            c => c.RoomId == newRoom.Id && c.StartDate < contract.EndDate && today < c.EndDate, ct);

        if (overlaps)
            throw new RoomDateOverlapException("Phòng mới bận trong khoảng thời gian còn lại của hợp đồng.");

        var hasActiveHold = await dbContext.Reservations.AnyAsync(
            r => r.RoomId == newRoom.Id
                 && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed),
            ct);

        if (hasActiveHold)
            throw new ConflictException("Phòng mới đang được người khác giữ.");

        contract.ChangeRoom(newRoom.Id);
        oldRoom.ChangeStatus(RoomStatus.Available);
        newRoom.ChangeStatus(RoomStatus.Occupied);

        await dbContext.SaveChangesAsync(ct);
    }
}
