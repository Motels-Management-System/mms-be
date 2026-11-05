namespace Mms.Application.Features.Contracts.Commands;

public sealed record AddContractTenantCommand(
    Guid ContractId,
    Guid TenantId,
    bool IsPrimary = false);
