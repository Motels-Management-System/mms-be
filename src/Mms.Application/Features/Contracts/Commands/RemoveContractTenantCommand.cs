namespace Mms.Application.Features.Contracts.Commands;

public sealed record RemoveContractTenantCommand(
    Guid ContractId,
    Guid TenantId);
