namespace Mms.Application.Features.Rooms.Commands;

public sealed record UpdateRoomCommand(
    Guid Id,
    string Code,
    int Floor,
    Guid RoomTypeId);
