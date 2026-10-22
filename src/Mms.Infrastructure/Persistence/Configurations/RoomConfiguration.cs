using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Status);

        // uint Version ánh xạ sang cột hệ thống xmin của PostgreSQL (optimistic concurrency).
        b.Property(x => x.Version).IsRowVersion();

        b.HasOne(x => x.RoomType)
            .WithMany()
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Amenities)
            .WithOne()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Amenities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
