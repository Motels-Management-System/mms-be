using Mms.Application.Features.Amenities.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Dtos;

public sealed record RoomDto(
    Guid Id,
    string Code,
    int Floor,
    RoomStatus Status,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal AreaM2,
    int MaxOccupancy,
    decimal ListPrice,
    IReadOnlyList<AmenityDto> Amenities,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
