using Mms.Domain.Common;

namespace Mms.Domain.Entities;

public class RoomType : AuditableEntity
{
    private RoomType() { }

    public RoomType(string name, decimal areaM2, int maxOccupancy, decimal listPrice, string? description = null)
        => Apply(name, areaM2, maxOccupancy, listPrice, description);

    public string Name { get; private set; } = default!;
    public decimal AreaM2 { get; private set; }
    public int MaxOccupancy { get; private set; }

    /// <summary>Giá niêm yết, chỉ để hiển thị (chưa dùng để tính tiền).</summary>
    public decimal ListPrice { get; private set; }

    public string? Description { get; private set; }

    public void Update(string name, decimal areaM2, int maxOccupancy, decimal listPrice, string? description)
        => Apply(name, areaM2, maxOccupancy, listPrice, description);

    private void Apply(string name, decimal areaM2, int maxOccupancy, decimal listPrice, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaM2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOccupancy);
        ArgumentOutOfRangeException.ThrowIfNegative(listPrice);

        Name = name.Trim();
        AreaM2 = areaM2;
        MaxOccupancy = maxOccupancy;
        ListPrice = listPrice;
        Description = description;
    }
}
