using Mms.Domain.Enums;

namespace Mms.Application.Features.MaintenanceRequests.Dtos;

public sealed record MaintenanceRequestDto(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    Guid CreatedByUserId,
    string Description,
    MaintenanceStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
