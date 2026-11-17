namespace Mms.Application.Features.Reports.Dtos;

/// <summary>
/// Tỷ lệ gia hạn và tỷ lệ quay vòng của các hợp đồng kết thúc trong [from, to).
/// Một hợp đồng được coi là gia hạn khi có hợp đồng khác cùng phòng bắt đầu đúng ngày kết thúc
/// và giữ lại ít nhất một người ở đang có trong hợp đồng cũ.
/// </summary>
public sealed record RenewalRateReportDto(
    DateOnly From,
    DateOnly To,
    int EndedContracts,
    int RenewedContracts,
    decimal RenewalRate,
    int TurnoverContracts,
    decimal TurnoverRate,
    int ReplacedByNewTenants,
    int LeftVacant,
    int EarlyTerminated);
