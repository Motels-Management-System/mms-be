using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> b)
    {
        b.ToTable("Contracts", t =>
            t.HasCheckConstraint("CK_Contracts_EndAfterStart", "\"EndDate\" > \"StartDate\""));
        b.HasKey(x => x.Id);

        b.Ignore(x => x.Period);

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Version).IsRowVersion();

        b.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Contract>().WithMany().HasForeignKey(x => x.RenewedFromContractId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Tenants)
            .WithOne()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Tenants).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Tìm phòng trống và kiểm tra trùng theo phòng + khoảng ngày.
        b.HasIndex(x => new { x.RoomId, x.StartDate, x.EndDate });

        // Dashboard: hợp đồng đang hiệu lực sắp hết hạn.
        b.HasIndex(x => new { x.Status, x.EndDate });

        // Ràng buộc chống chồng lấn (EXCLUDE USING gist) được thêm bằng migration SQL thủ công,
        // vì EF Core không có API fluent cho exclusion constraint.
    }
}
