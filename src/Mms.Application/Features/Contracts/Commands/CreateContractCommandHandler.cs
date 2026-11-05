using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;
using Mms.Domain.ValueObjects;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class CreateContractCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IOptions<RentalOptions> rentalOptions,
    IValidator<CreateContractCommand> validator) : ICommandHandler<CreateContractCommand, ContractDetailDto>
{
    public async Task<ContractDetailDto> HandleAsync(CreateContractCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct)
            ?? throw new NotFoundException("Phòng", command.RoomId);

        if (room.Status == RoomStatus.Maintenance)
            throw new DomainException("Phòng đang bảo trì, không thể tạo hợp đồng.");

        if (command.Months < rentalOptions.Value.MinContractMonths)
            throw new DomainException($"Thời hạn hợp đồng tối thiểu là {rentalOptions.Value.MinContractMonths} tháng.");

        var period = DateRange.FromMonths(command.StartDate, command.Months);

        // Kiểm tra thân thiện để trả thông báo rõ ràng; exclusion constraint là lớp bảo vệ thật sự.
        var overlaps = await dbContext.Contracts.AsNoTracking().AnyAsync(
            c => c.RoomId == room.Id && c.StartDate < period.End && period.Start < c.EndDate, ct);

        if (overlaps)
            throw new RoomDateOverlapException("Khoảng thời gian thuê trùng với hợp đồng khác của phòng.");

        if (command.Tenants.Count(t => t.IsPrimary) != 1)
            throw new DomainException("Hợp đồng cần đúng một người đại diện.");

        var tenantIds = command.Tenants.Select(t => t.TenantId).Distinct().ToList();

        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => (t.FullName, t.Phone), ct);

        if (tenants.Count != tenantIds.Count)
            throw new NotFoundException("Một hoặc nhiều người thuê không tồn tại.");

        Reservation? reservation = null;
        if (command.ReservationId.HasValue)
        {
            reservation = await dbContext.Reservations
                .FirstOrDefaultAsync(r => r.Id == command.ReservationId.Value, ct)
                ?? throw new NotFoundException("Giữ phòng", command.ReservationId.Value);

            if (reservation.RoomId != room.Id)
                throw new DomainException("Giữ phòng không thuộc phòng của hợp đồng.");

            reservation.MarkConverted();
        }

        var contract = new Contract(room.Id, period, command.ReservationId);
        foreach (var tenant in command.Tenants)
        {
            contract.AddTenant(tenant.TenantId, tenant.IsPrimary, room.RoomType.MaxOccupancy);
        }

        dbContext.Contracts.Add(contract);
        // Một lần SaveChanges duy nhất: hợp đồng + giữ phòng chuyển trạng thái được ghi cùng nhau,
        // conflict 23P01 (exclusion constraint) sẽ được middleware trả về 409 ROOM_DATE_OVERLAP.
        await dbContext.SaveChangesAsync(ct);

        return ContractDtoMapper.ToDetail(contract, room.Code, clock, tenantLookup: tenants);
    }
}
