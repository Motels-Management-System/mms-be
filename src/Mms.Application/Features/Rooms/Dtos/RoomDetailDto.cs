using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Dtos;

public sealed record RoomDetailDto(
    Guid Id,
    string Code,
    int Floor,
    RoomStatus Status,
    RoomTypeDto RoomType,
    IReadOnlyList<AmenityDto> Amenities,
    CurrentContractDto? CurrentContract,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
