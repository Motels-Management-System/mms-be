using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Dtos;

namespace Mms.Application.Features.MaintenanceRequests.Queries;

public sealed class GetMaintenanceRequestByIdQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetMaintenanceRequestByIdQuery> validator)
    : IQueryHandler<GetMaintenanceRequestByIdQuery, MaintenanceRequestDto>
{
    public async Task<MaintenanceRequestDto> HandleAsync(
        GetMaintenanceRequestByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var request = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct)
            ?? throw new NotFoundException("Yêu cầu bảo trì", query.Id);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException("Phòng", request.RoomId);

        if (!query.IsStaffOrAdmin)
        {
            var mine = await GetMyRoomIdsAsync(query.CurrentUserId, ct);
            var isMine = request.CreatedByUserId == query.CurrentUserId || mine.Contains(request.RoomId);

            if (!isMine)
                throw new ForbiddenException("Chỉ nhân viên hoặc người liên quan mới xem được yêu cầu này.");
        }

        return new MaintenanceRequestDto(
            request.Id,
            request.RoomId,
            room.Code,
            request.CreatedByUserId,
            request.Description,
            request.Status,
            request.CreatedAt,
            request.UpdatedAt);
    }

    private async Task<List<Guid>> GetMyRoomIdsAsync(Guid currentUserId, CancellationToken ct)
    {
        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.UserId == currentUserId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        if (tenantId is null)
            return [];

        return await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Tenants.Any(t => t.TenantId == tenantId.Value))
            .Select(c => c.RoomId)
            .Distinct()
            .ToListAsync(ct);
    }
}
