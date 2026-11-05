using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reservations.Dtos;

namespace Mms.Application.Features.Reservations.Queries;

public sealed class GetReservationByIdQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetReservationByIdQuery> validator) : IQueryHandler<GetReservationByIdQuery, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(GetReservationByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var row = await dbContext.Reservations
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .Select(r => new
            {
                r.Id,
                r.RoomId,
                RoomCode = dbContext.Rooms.Where(rm => rm.Id == r.RoomId).Select(rm => rm.Code).First(),
                r.TenantId,
                TenantFullName = dbContext.Tenants.Where(t => t.Id == r.TenantId).Select(t => t.FullName).First(),
                TenantPhone = dbContext.Tenants.Where(t => t.Id == r.TenantId).Select(t => t.Phone).First(),
                TenantUserId = dbContext.Tenants.Where(t => t.Id == r.TenantId).Select(t => (Guid?)t.UserId).First(),
                r.Status,
                r.ExpiresAt,
                r.CreatedAt,
                r.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            throw new NotFoundException("Giữ phòng", query.Id);

        if (!query.IsStaffOrAdmin && row.TenantUserId != query.CurrentUserId)
            throw new ForbiddenException("Chỉ nhân viên hoặc chủ giữ phòng mới được xem.");

        return new ReservationDto(
            row.Id, row.RoomId, row.RoomCode, row.TenantId, row.TenantFullName, row.TenantPhone,
            row.Status, row.ExpiresAt, row.CreatedAt, row.UpdatedAt);
    }
}
