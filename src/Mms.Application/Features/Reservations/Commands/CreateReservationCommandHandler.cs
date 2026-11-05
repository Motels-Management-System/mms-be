using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reservations.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Reservations.Commands;

public sealed class CreateReservationCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IOptions<RentalOptions> rentalOptions,
    IValidator<CreateReservationCommand> validator) : ICommandHandler<CreateReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        Tenant tenant;
        if (command.TenantId.HasValue)
        {
            if (!command.IsStaffOrAdmin)
                throw new ForbiddenException("Chỉ nhân viên được tạo giữ phòng hộ người khác.");

            tenant = await dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == command.TenantId.Value, ct)
                ?? throw new NotFoundException("Người thuê", command.TenantId.Value);
        }
        else
        {
            tenant = await dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == command.CurrentUserId, ct)
                ?? throw new DomainException("Tài khoản chưa có hồ sơ người thuê.");
        }

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct)
            ?? throw new NotFoundException("Phòng", command.RoomId);

        // Kiểm tra thân thiện trước khi vào DB; unique index UX_Reservations_ActiveHold_Room
        // (bắt bởi middleware thành 409) là lớp bảo vệ thật sự khi có request đồng thời.
        if (room.Status != RoomStatus.Available)
            throw new ConflictException("Phòng không còn trống để giữ.");

        var hasActiveHold = await dbContext.Reservations.AnyAsync(
            r => r.RoomId == room.Id
                 && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed),
            ct);

        if (hasActiveHold)
            throw new ConflictException("Phòng đang được người khác giữ.");

        var reservation = Reservation.Create(
            room.Id,
            tenant.Id,
            command.CurrentUserId,
            clock.UtcNow,
            TimeSpan.FromHours(rentalOptions.Value.ReservationHoldHours));

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(ct);

        return new ReservationDto(
            reservation.Id,
            reservation.RoomId,
            room.Code,
            tenant.Id,
            tenant.FullName,
            tenant.Phone,
            reservation.Status,
            reservation.ExpiresAt,
            reservation.CreatedAt,
            reservation.UpdatedAt);
    }
}
