using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> b)
    {
        b.ToTable("RoomTypes");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.AreaM2).HasPrecision(8, 2);
        b.Property(x => x.ListPrice).HasPrecision(14, 2);
        b.Property(x => x.Description).HasMaxLength(1000);
    }
}
