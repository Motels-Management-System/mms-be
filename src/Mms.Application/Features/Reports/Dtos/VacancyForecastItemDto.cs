using Mms.Domain.Enums;

namespace Mms.Application.Features.Reports.Dtos;

/// <summary>
/// Phòng sẽ trống trong N ngày tới: hợp đồng đang hiệu lực kết thúc trong kỳ và chưa có hợp đồng
/// nào (kể cả Draft) bắt đầu đúng ngày đó để nối tiếp. NextContractStartDate là ngày bắt đầu của
/// hợp đồng kế tiếp nếu đã được lên lịch muộn hơn, tức phòng vẫn trống trong khoảng chờ.
/// </summary>
public sealed record VacancyForecastItemDto(
    Guid RoomId,
    string Code,
    int Floor,
    Guid RoomTypeId,
    string RoomTypeName,
    RoomStatus Status,
    Guid ContractId,
    DateOnly EndDate,
    int DaysUntilVacant,
    string? PrimaryTenantName,
    DateOnly? NextContractStartDate);
