using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Application.Features.Rooms.Dtos;

namespace Mms.Application.Features.Contracts.Queries;

public sealed class GetContractByIdQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetContractByIdQuery> validator) : IQueryHandler<GetContractByIdQuery, ContractDetailDto>
{
    public async Task<ContractDetailDto> HandleAsync(GetContractByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == query.Id, ct)
            ?? throw new NotFoundException("Hợp đồng", query.Id);

        if (!query.IsStaffOrAdmin)
        {
            var tenantIds = contract.Tenants.Select(t => t.TenantId).ToList();

            var isTenant = await dbContext.Tenants
                .AsNoTracking()
                .AnyAsync(t => t.UserId == query.CurrentUserId && tenantIds.Contains(t.Id), ct);

            if (!isTenant)
                throw new ForbiddenException("Chỉ nhân viên hoặc người ở trong hợp đồng mới được xem.");
        }

        var roomCode = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == contract.RoomId)
            .Select(r => r.Code)
            .FirstAsync(ct);

        var tenantIds2 = contract.Tenants.Select(t => t.TenantId).ToList();
        var tenantLookup = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds2.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => (t.FullName, t.Phone), ct);

        var handovers = await dbContext.HandoverRecords
            .AsNoTracking()
            .Where(h => h.ContractId == contract.Id)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(ct);

        return ContractDtoMapper.ToDetail(contract, roomCode, clock, handovers, tenantLookup);
    }
}
