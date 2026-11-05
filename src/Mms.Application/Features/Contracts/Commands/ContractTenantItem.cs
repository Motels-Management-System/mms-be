namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Người ở thêm vào hợp đồng khi tạo. Phải có đúng một người đại diện (IsPrimary).</summary>
public sealed record ContractTenantItem(
    Guid TenantId,
    bool IsPrimary = false);
