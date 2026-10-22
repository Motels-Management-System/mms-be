namespace Mms.Domain.Entities;

/// <summary>Người ở trong hợp đồng (nhiều-nhiều). IsPrimary = người đại diện ký hợp đồng.</summary>
public class ContractTenant
{
    private ContractTenant() { }

    public ContractTenant(Guid contractId, Guid tenantId, bool isPrimary)
    {
        ContractId = contractId;
        TenantId = tenantId;
        IsPrimary = isPrimary;
    }

    public Guid ContractId { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsPrimary { get; private set; }
}
