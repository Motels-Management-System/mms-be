using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Domain.Common;
using Mms.Domain.Entities;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider clock)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractTenant> ContractTenants => Set<ContractTenant>();
    public DbSet<HandoverRecord> HandoverRecords => Set<HandoverRecord>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cần cho exclusion constraint chống chồng lấn (so sánh uuid bằng "=" trong index gist).
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Id sinh ở phía ứng dụng (Guid v7) nên EF không được coi là giá trị do DB sinh.
        // Nếu không, entity con có Id sẵn thêm vào entity cha đang theo dõi có thể bị hiểu nhầm là "Modified".
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.Id))
                .ValueGeneratedNever();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.MarkCreated(now);
            else if (entry.State == EntityState.Modified) entry.Entity.MarkUpdated(now);
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
