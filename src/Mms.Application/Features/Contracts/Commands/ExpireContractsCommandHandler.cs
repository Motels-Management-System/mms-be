using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Commands;

public sealed class ExpireContractsCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock) : ICommandHandler<ExpireContractsCommand, int>
{
    /// <summary>Trả về số hợp đồng vừa chuyển sang Expired.</summary>
    public async Task<int> HandleAsync(ExpireContractsCommand command, CancellationToken ct)
    {
        var today = clock.Today;

        var due = await dbContext.Contracts
            .Where(c => c.Status == ContractStatus.Active && c.EndDate <= today)
            .ToListAsync(ct);

        if (due.Count == 0)
            return 0;

        var expiredContracts = due.Where(c => c.TryExpire(today)).ToList();
        if (expiredContracts.Count == 0)
            return 0;

        var expiredIds = expiredContracts.Select(c => c.Id).ToList();

        var renewals = await dbContext.Contracts
            .Include(c => c.Tenants)
            .Where(c => c.Status == ContractStatus.Draft
                        && c.RenewedFromContractId != null
                        && expiredIds.Contains(c.RenewedFromContractId.Value))
            .ToListAsync(ct);

        if (renewals.Count > 0)
        {
            foreach (var renewal in renewals)
                await ActivateRenewalIfSeamlessAsync(renewal, expiredContracts, ct);
        }

        await ConcurrencyAwareSave.RetryOnceAsync(dbContext, ct);
        return expiredContracts.Count;
    }

    private async Task ActivateRenewalIfSeamlessAsync(
        Contract renewal,
        IReadOnlyList<Contract> expiredContracts,
        CancellationToken ct)
    {
        var original = expiredContracts.First(c => c.Id == renewal.RenewedFromContractId);

        // Chỉ nối tiếp liền mạch khi hợp đồng mới bắt đầu đúng ngày phòng trống lại.
        if (renewal.StartDate != original.EndDate)
            return;

        // Không có người đại diện thì Activation sẽ bị DomainException; bỏ qua, nhân viên check-in thủ công.
        if (!renewal.Tenants.Any(t => t.IsPrimary))
            return;

        renewal.Activate();

        var room = await dbContext.Rooms.FirstOrDefaultAsync(r => r.Id == renewal.RoomId, ct);

        // Phòng thường đang Occupied (hợp đồng cũ chưa check-out); nếu bị trạng thái khác thì chuyển lại
        // để khớp với hợp đồng đang hiệu lực. Phòng Maintenance phải do nhân viên xử lý.
        if (room is not null && room.Status is RoomStatus.Available or RoomStatus.Reserved)
            room.ChangeStatus(RoomStatus.Occupied);
    }
}
