using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Queries;

public sealed class GetRoomByIdQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetRoomByIdQuery> validator) : IQueryHandler<GetRoomByIdQuery, RoomDetailDto>
{
    public async Task<RoomDetailDto> HandleAsync(GetRoomByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct);

        if (room is null)
            throw new NotFoundException("Phòng", query.Id);

        // Xếp hạng trên cột của entity rồi mới dựng DTO: OrderBy trên DTO không dịch được sang SQL.
        var amenityRows = await dbContext.RoomAmenities
            .AsNoTracking()
            .Where(ra => ra.RoomId == query.Id)
            .Join(
                dbContext.Amenities.AsNoTracking(),
                ra => ra.AmenityId,
                a => a.Id,
                (ra, a) => new { a.Id, a.Name, a.CreatedAt, a.UpdatedAt })
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        var amenities = amenityRows
            .Select(a => new AmenityDto(a.Id, a.Name, a.CreatedAt, a.UpdatedAt))
            .ToList();

        CurrentContractDto? currentContract = null;
        if (query.IncludeCurrentContract)
        {
            var today = clock.Today;
            var contract = await dbContext.Contracts
                .AsNoTracking()
                .Where(c => c.RoomId == query.Id && c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync(ct);

            if (contract is not null)
            {
                var tenantRows = await dbContext.ContractTenants
                    .AsNoTracking()
                    .Where(ctt => ctt.ContractId == contract.Id)
                    .Join(
                        dbContext.Tenants.AsNoTracking(),
                        ctt => ctt.TenantId,
                        t => t.Id,
                        (ctt, t) => new { t.Id, t.FullName, t.Phone, ctt.IsPrimary })
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.FullName)
                    .ToListAsync(ct);

                var tenants = tenantRows
                    .Select(x => new ContractTenantSummaryDto(x.Id, x.FullName, x.Phone, x.IsPrimary))
                    .ToList();

                currentContract = new CurrentContractDto(
                    contract.Id,
                    contract.StartDate,
                    contract.EndDate,
                    contract.Status,
                    contract.DaysRemaining(today),
                    tenants);
            }
        }

        var roomTypeDto = new RoomTypeDto(
            room.RoomType.Id,
            room.RoomType.Name,
            room.RoomType.AreaM2,
            room.RoomType.MaxOccupancy,
            room.RoomType.ListPrice,
            room.RoomType.Description,
            room.RoomType.CreatedAt,
            room.RoomType.UpdatedAt);

        return new RoomDetailDto(
            room.Id,
            room.Code,
            room.Floor,
            room.Status,
            roomTypeDto,
            amenities,
            currentContract,
            room.CreatedAt,
            room.UpdatedAt);
    }
}
