using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mms.Application.Abstractions;
using Mms.Domain.Common;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Ghi một dòng AuditLog cho mỗi thay đổi của Room, Contract, Reservation, Tenant, MaintenanceRequest,
/// kèm giá trị cũ/mới dạng JSON để tra cứu trong /api/audit-logs.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    ICurrentUserProvider currentUser,
    IDateTimeProvider clock) : SaveChangesInterceptor
{
    private static readonly Type[] AuditedTypes =
    [
        typeof(Room), typeof(Contract), typeof(Reservation), typeof(Tenant), typeof(MaintenanceRequest)
    ];

    // Cột kỹ thuật: Id không đổi, CreatedAt/UpdatedAt do AppDbContext gán, Version là xmin.
    private static readonly HashSet<string> IgnoredProperties =
    [
        nameof(BaseEntity.Id),
        nameof(AuditableEntity.CreatedAt),
        nameof(AuditableEntity.UpdatedAt),
        "Version"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditedTypes.Contains(e.Entity.GetType()) && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return;

        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in entries)
        {
            var changes = ChangedProperties(entry);
            if (entry.State == EntityState.Modified && changes.Count == 0) continue;

            var (action, oldValues, newValues) = entry.State switch
            {
                EntityState.Added => ("Created", null, Serialize(changes.Select(c => (c.Name, c.NewValue)))),
                EntityState.Deleted => ("Deleted", Serialize(changes.Select(c => (c.Name, c.OldValue))), null),
                _ => ("Updated",
                    Serialize(changes.Select(c => (c.Name, c.OldValue))),
                    Serialize(changes.Select(c => (c.Name, c.NewValue))))
            };

            context.Set<AuditLog>().Add(new AuditLog(
                userId,
                entry.Entity.GetType().Name,
                ((BaseEntity)entry.Entity).Id.ToString(),
                action,
                oldValues,
                newValues,
                now));
        }
    }

    private static List<(string Name, object? OldValue, object? NewValue)> ChangedProperties(EntityEntry entry)
        => entry.Properties
            .Where(p => !IgnoredProperties.Contains(p.Metadata.Name))
            .Where(p => entry.State != EntityState.Modified || p.IsModified)
            .Select(p => (p.Metadata.Name, p.OriginalValue, p.CurrentValue))
            .ToList();

    private static string? Serialize(IEnumerable<(string Name, object? Value)> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return null;

        var map = new Dictionary<string, object?>(list.Count);
        foreach (var (name, value) in list)
            map[name] = value;

        return JsonSerializer.Serialize(map, JsonOptions);
    }
}
