namespace Mms.Application.Features.RoomTypes.Dtos;

public sealed record RoomTypeDto(
    Guid Id,
    string Name,
    decimal AreaM2,
    int MaxOccupancy,
    decimal ListPrice,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
