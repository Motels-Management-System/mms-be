using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class MeterReadingConfiguration : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> b)
    {
        b.ToTable("MeterReadings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Electricity).HasPrecision(12, 2);
        b.Property(x => x.Water).HasPrecision(12, 2);

        b.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);

        // Mỗi phòng chỉ có một bản ghi cho mỗi tháng.
        b.HasIndex(x => new { x.RoomId, x.PeriodMonth }).IsUnique();
    }
}
