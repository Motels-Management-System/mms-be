using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MeterReadings.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.MeterReadings.Commands;

public sealed class CreateMeterReadingCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<CreateMeterReadingCommand> validator)
    : ICommandHandler<CreateMeterReadingCommand, MeterReadingDto>
{
    public async Task<MeterReadingDto> HandleAsync(CreateMeterReadingCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct)
            ?? throw new NotFoundException("Phòng", command.RoomId);

        var periodMonth = MeterReadingPeriodRules.FirstDayOfMonth(command.PeriodMonth);

        if (periodMonth > MeterReadingPeriodRules.FirstDayOfMonth(clock.Today))
            throw new DomainException("Không ghi được chỉ số cho tháng trong tương lai.");

        // Kiểm tra thân thiện trước; unique index (RoomId, PeriodMonth) bắt 23505 → 409 là lớp bảo vệ thật sự.
        var periodExists = await dbContext.MeterReadings.AnyAsync(
            m => m.RoomId == room.Id && m.PeriodMonth == periodMonth, ct);

        if (periodExists)
            throw new ConflictException($"Phòng {room.Code} đã có chỉ số tháng {periodMonth:MM/yyyy}.");

        var previous = await dbContext.MeterReadings
            .AsNoTracking()
            .Where(m => m.RoomId == room.Id && m.PeriodMonth < periodMonth)
            .OrderByDescending(m => m.PeriodMonth)
            .FirstOrDefaultAsync(ct);

        var reading = new MeterReading(
            room.Id,
            periodMonth,
            command.Electricity,
            command.Water,
            previous,
            command.IsMeterReplaced);

        dbContext.MeterReadings.Add(reading);
        await dbContext.SaveChangesAsync(ct);

        return new MeterReadingDto(
            reading.Id,
            reading.RoomId,
            room.Code,
            reading.PeriodMonth,
            reading.Electricity,
            reading.Water,
            reading.IsMeterReplaced,
            reading.CreatedAt,
            reading.UpdatedAt);
    }
}
