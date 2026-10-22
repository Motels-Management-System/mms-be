using Mms.Domain.Common;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;
using Mms.Domain.ValueObjects;

namespace Mms.Domain.Entities;

public class Contract : AuditableEntity
{
    private readonly List<ContractTenant> _tenants = [];

    private Contract() { }

    public Contract(Guid roomId, DateRange period, Guid? reservationId = null)
    {
        RoomId = roomId;
        ReservationId = reservationId;
        StartDate = period.Start;
        EndDate = period.End;
        Status = ContractStatus.Draft;
    }

    public Guid RoomId { get; private set; }
    public Guid? ReservationId { get; private set; }

    /// <summary>Ngày bắt đầu (tính cả ngày này).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Ngày kết thúc (loại trừ): phòng trống lại từ ngày này.</summary>
    public DateOnly EndDate { get; private set; }

    public ContractStatus Status { get; private set; }
    public uint Version { get; private set; }

    public DateRange Period => new(StartDate, EndDate);
    public IReadOnlyCollection<ContractTenant> Tenants => _tenants;

    /// <summary>"Sắp hết hạn" là giá trị tính toán từ ngày hiện tại, không lưu thành cột.</summary>
    public int DaysRemaining(DateOnly today) => Period.DaysUntilEnd(today);

    public bool IsExpiringWithin(DateOnly today, int days)
        => Status == ContractStatus.Active && DaysRemaining(today) is >= 0 && DaysRemaining(today) <= days;

    public void AddTenant(Guid tenantId, bool isPrimary, int maxOccupancy)
    {
        if (Status is not (ContractStatus.Draft or ContractStatus.Active))
            throw new DomainException("Không thể thêm người ở vào hợp đồng đã kết thúc.");

        if (_tenants.Any(t => t.TenantId == tenantId))
            throw new DomainException("Người này đã có trong hợp đồng.");

        if (_tenants.Count >= maxOccupancy)
            throw new DomainException($"Vượt quá sức chứa tối đa của phòng ({maxOccupancy} người).");

        if (isPrimary && _tenants.Any(t => t.IsPrimary))
            throw new DomainException("Hợp đồng đã có người đại diện.");

        _tenants.Add(new ContractTenant(Id, tenantId, isPrimary));
    }

    /// <summary>Draft → Active (nhận phòng).</summary>
    public void Activate()
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException("Chỉ kích hoạt được hợp đồng ở trạng thái Draft.");

        if (!_tenants.Any(t => t.IsPrimary))
            throw new DomainException("Hợp đồng cần có người đại diện trước khi kích hoạt.");

        Status = ContractStatus.Active;
    }

    /// <summary>Dùng cho background job. Trả về true nếu vừa chuyển sang Expired.</summary>
    public bool TryExpire(DateOnly today)
    {
        if (Status != ContractStatus.Active || today < EndDate) return false;
        Status = ContractStatus.Expired;
        return true;
    }

    /// <summary>
    /// Chấm dứt sớm: rút ngắn EndDate về ngày chấm dứt để khoảng thời gian thực tế
    /// phòng đã bị chiếm vẫn được giữ trong ràng buộc chống chồng lấn.
    /// </summary>
    public void Terminate(DateOnly terminationDate)
    {
        if (Status != ContractStatus.Active)
            throw new DomainException("Chỉ chấm dứt sớm được hợp đồng đang Active.");

        if (terminationDate <= StartDate || terminationDate >= EndDate)
            throw new DomainException("Ngày chấm dứt phải nằm sau ngày bắt đầu và trước ngày hết hạn.");

        EndDate = terminationDate;
        Status = ContractStatus.Terminated;
    }

    /// <summary>Gia hạn = hợp đồng mới nối tiếp đúng ngày hết hạn, không có khoảng hở hay chồng lấn.</summary>
    public Contract CreateRenewal(int months)
    {
        if (Status != ContractStatus.Active)
            throw new DomainException("Chỉ gia hạn được hợp đồng đang Active.");

        var renewal = new Contract(RoomId, DateRange.FromMonths(EndDate, months));
        foreach (var t in _tenants)
            renewal._tenants.Add(new ContractTenant(renewal.Id, t.TenantId, t.IsPrimary));

        return renewal;
    }
}
