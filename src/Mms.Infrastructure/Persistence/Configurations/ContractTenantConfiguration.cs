using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class ContractTenantConfiguration : IEntityTypeConfiguration<ContractTenant>
{
    public void Configure(EntityTypeBuilder<ContractTenant> b)
    {
        b.ToTable("ContractTenants");
        b.HasKey(x => new { x.ContractId, x.TenantId });

        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
