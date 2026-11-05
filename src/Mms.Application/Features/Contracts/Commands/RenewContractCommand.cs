namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Gia hạn: tạo hợp đồng Draft mới nối tiếp đúng EndDate của hợp đồng đang Active.</summary>
public sealed record RenewContractCommand(
    Guid ContractId,
    int Months);
