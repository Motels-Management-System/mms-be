namespace Mms.Application.Features.MeterReadings.Queries;

public sealed record GetMeterReadingsQuery(
    Guid RoomId,
    int Page = 1,
    int PageSize = 20);
