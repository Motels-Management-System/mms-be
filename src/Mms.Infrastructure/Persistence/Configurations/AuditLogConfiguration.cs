using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);

        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Action).HasMaxLength(20).IsRequired();
        b.Property(x => x.OldValues).HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnType("jsonb");

        // Không đặt khóa ngoại tới user: giữ nhật ký ngay cả khi tài khoản bị xóa.
        b.HasIndex(x => new { x.EntityName, x.EntityId });
        b.HasIndex(x => x.CreatedAt);
    }
}
