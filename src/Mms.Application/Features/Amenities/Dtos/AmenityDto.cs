namespace Mms.Application.Features.Amenities.Dtos;

public sealed record AmenityDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
