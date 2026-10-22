using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("Reservations");
        b.HasKey(x => x.Id);

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.Version).IsRowVersion();

        b.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Mỗi phòng chỉ có tối đa một giữ phòng còn hiệu lực tại một thời điểm.
        b.HasIndex(x => x.RoomId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Confirmed')")
            .HasDatabaseName("UX_Reservations_ActiveHold_Room");

        // Phục vụ background job quét giữ phòng hết hạn.
        b.HasIndex(x => new { x.Status, x.ExpiresAt });
    }
}
