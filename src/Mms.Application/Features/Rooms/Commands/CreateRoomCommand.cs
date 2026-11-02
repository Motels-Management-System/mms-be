namespace Mms.Application.Features.Rooms.Commands;

public sealed record CreateRoomCommand(
    string Code,
    int Floor,
    Guid RoomTypeId,
    IReadOnlyList<Guid>? AmenityIds = null);
