namespace Mms.Application.Features.MeterReadings.Dtos;

public sealed record MeterReadingDto(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    DateOnly PeriodMonth,
    decimal Electricity,
    decimal Water,
    bool IsMeterReplaced,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
