namespace Mms.Application.Features.MeterReadings.Dtos;

/// <summary>Phòng có người ở trong kỳ nhưng chưa được ghi chỉ số điện nước.</summary>
public sealed record MissingMeterReadingDto(
    Guid RoomId,
    string RoomCode,
    DateOnly PeriodMonth,
    Guid ContractId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? PrimaryTenantName);
