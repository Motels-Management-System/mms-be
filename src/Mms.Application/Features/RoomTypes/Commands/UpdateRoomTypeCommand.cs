namespace Mms.Application.Features.RoomTypes.Commands;

public sealed record UpdateRoomTypeCommand(
    Guid Id,
    string Name,
    decimal AreaM2,
    int MaxOccupancy,
    decimal ListPrice,
    string? Description);
