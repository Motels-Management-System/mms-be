using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class HandoverRecordConfiguration : IEntityTypeConfiguration<HandoverRecord>
{
    public void Configure(EntityTypeBuilder<HandoverRecord> b)
    {
        b.ToTable("HandoverRecords");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.RoomCondition).HasMaxLength(500).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.HasOne<Contract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ContractId, x.Type });
    }
}
