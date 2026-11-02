using Mms.Domain.Common;

namespace Mms.Domain.Entities;

public class Amenity : AuditableEntity
{
    private Amenity() { }

    public Amenity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public string Name { get; private set; } = default!;

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
