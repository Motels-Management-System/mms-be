using Microsoft.EntityFrameworkCore;
using Mms.Domain.Entities;

namespace Mms.Application.Abstractions;

/// <summary>Cổng truy cập dữ liệu cho tầng Application. Infrastructure cài đặt bằng AppDbContext.</summary>
public interface IAppDbContext
{
    DbSet<RoomType> RoomTypes { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Amenity> Amenities { get; }
    DbSet<RoomAmenity> RoomAmenities { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractTenant> ContractTenants { get; }
    DbSet<MeterReading> MeterReadings { get; }
    DbSet<MaintenanceRequest> MaintenanceRequests { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
