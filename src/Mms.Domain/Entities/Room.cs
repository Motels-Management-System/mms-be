using Mms.Domain.Common;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Domain.Entities;

public class Room : AuditableEntity
{
    // Các chuyển trạng thái hợp lệ, khớp với sơ đồ trạng thái phòng trong tài liệu.
    private static readonly Dictionary<RoomStatus, RoomStatus[]> AllowedTransitions = new()
    {
        [RoomStatus.Available] = [RoomStatus.Reserved, RoomStatus.Occupied, RoomStatus.Maintenance],
        [RoomStatus.Reserved] = [RoomStatus.Available, RoomStatus.Occupied],
        [RoomStatus.Occupied] = [RoomStatus.Available, RoomStatus.Maintenance],
        [RoomStatus.Maintenance] = [RoomStatus.Available],
    };

    private readonly List<RoomAmenity> _amenities = [];

    private Room() { }

    public Room(string code, int floor, Guid roomTypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code.Trim().ToUpperInvariant();
        Floor = floor;
        RoomTypeId = roomTypeId;
        Status = RoomStatus.Available;
    }

    public string Code { get; private set; } = default!;
    public int Floor { get; private set; }
    public RoomStatus Status { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public RoomType RoomType { get; private set; } = default!;

    /// <summary>Ánh xạ sang cột hệ thống xmin của PostgreSQL, dùng làm optimistic concurrency token.</summary>
    public uint Version { get; private set; }

    public IReadOnlyCollection<RoomAmenity> Amenities => _amenities;

    public void ChangeStatus(RoomStatus next)
    {
        if (next == Status) return;

        if (!AllowedTransitions[Status].Contains(next))
            throw new DomainException($"Không thể chuyển phòng {Code} từ {Status} sang {next}.");

        Status = next;
    }

    public void AddAmenity(Guid amenityId)
    {
        if (_amenities.Any(a => a.AmenityId == amenityId)) return;
        _amenities.Add(new RoomAmenity(Id, amenityId));
    }

    public void Update(string code, int floor, Guid roomTypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (roomTypeId == Guid.Empty)
            throw new ArgumentException("Loại phòng không hợp lệ.", nameof(roomTypeId));

        Code = code.Trim().ToUpperInvariant();
        Floor = floor;
        RoomTypeId = roomTypeId;
    }

    public void UpdateAmenities(IEnumerable<Guid> amenityIds)
    {
        _amenities.Clear();
        foreach (var id in amenityIds.Distinct())
        {
            if (id != Guid.Empty)
            {
                _amenities.Add(new RoomAmenity(Id, id));
            }
        }
    }
}
