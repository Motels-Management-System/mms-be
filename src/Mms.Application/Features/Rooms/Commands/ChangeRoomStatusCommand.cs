using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Commands;

public sealed record ChangeRoomStatusCommand(
    Guid RoomId,
    RoomStatus Status);
