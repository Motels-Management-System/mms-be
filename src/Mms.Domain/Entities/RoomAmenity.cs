namespace Mms.Domain.Entities;

/// <summary>Bảng nối nhiều-nhiều giữa phòng và tiện nghi (khóa chính kép).</summary>
public class RoomAmenity
{
    private RoomAmenity() { }

    public RoomAmenity(Guid roomId, Guid amenityId)
    {
        RoomId = roomId;
        AmenityId = amenityId;
    }

    public Guid RoomId { get; private set; }
    public Guid AmenityId { get; private set; }
}
