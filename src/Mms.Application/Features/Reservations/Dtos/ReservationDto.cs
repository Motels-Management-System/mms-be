using Mms.Domain.Enums;

namespace Mms.Application.Features.Reservations.Dtos;

public sealed record ReservationDto(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    Guid TenantId,
    string TenantFullName,
    string TenantPhone,
    ReservationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
