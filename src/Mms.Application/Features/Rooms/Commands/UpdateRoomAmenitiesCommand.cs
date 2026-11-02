namespace Mms.Application.Features.Rooms.Commands;

public sealed record UpdateRoomAmenitiesCommand(
    Guid RoomId,
    IReadOnlyList<Guid> AmenityIds);
