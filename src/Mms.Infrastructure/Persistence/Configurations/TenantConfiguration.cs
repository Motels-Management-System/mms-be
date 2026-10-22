using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Phone);

        // Một tài khoản liên kết tối đa một hồ sơ người thuê. UserId để trống được
        // (PostgreSQL cho phép nhiều NULL trong unique index).
        b.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Tenant>(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
