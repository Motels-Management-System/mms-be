using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Queries;

public sealed record GetRoomsQuery(
    RoomStatus? Status = null,
    Guid? RoomTypeId = null,
    int? Floor = null,
    string? Keyword = null,
    int Page = 1,
    int PageSize = 20);
