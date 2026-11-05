namespace Mms.Application.Features.Rooms.Queries;

/// <summary>Tìm phòng trống trong khoảng [From, To) cho số khách Guests.</summary>
public sealed record GetAvailableRoomsQuery(
    DateOnly From,
    DateOnly To,
    int Guests = 1,
    Guid? RoomTypeId = null,
    IReadOnlyList<Guid>? AmenityIds = null,
    int Page = 1,
    int PageSize = 20);
