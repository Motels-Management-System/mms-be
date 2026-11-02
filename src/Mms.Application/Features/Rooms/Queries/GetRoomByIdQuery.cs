namespace Mms.Application.Features.Rooms.Queries;

public sealed record GetRoomByIdQuery(Guid Id, bool IncludeCurrentContract = false);
