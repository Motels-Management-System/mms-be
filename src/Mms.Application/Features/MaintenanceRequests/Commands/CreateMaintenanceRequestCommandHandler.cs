using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.MaintenanceRequests.Commands;

public sealed class CreateMaintenanceRequestCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<CreateMaintenanceRequestCommand> validator)
    : ICommandHandler<CreateMaintenanceRequestCommand, MaintenanceRequestDto>
{
    public async Task<MaintenanceRequestDto> HandleAsync(
        CreateMaintenanceRequestCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct)
            ?? throw new NotFoundException("Phòng", command.RoomId);

        if (!command.IsStaffOrAdmin)
        {
            var today = clock.Today;

            var tenantId = await dbContext.Tenants
                .AsNoTracking()
                .Where(t => t.UserId == command.CurrentUserId)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync(ct);

            var livesHere = tenantId is not null && await dbContext.Contracts
                .AsNoTracking()
                .AnyAsync(c => c.RoomId == room.Id
                               && c.Status == ContractStatus.Active
                               && c.StartDate <= today
                               && c.EndDate > today
                               && c.Tenants.Any(t => t.TenantId == tenantId.Value), ct);

            if (!livesHere)
                throw new ForbiddenException("Chỉ tạo được yêu cầu bảo trì cho phòng mình đang thuê.");
        }

        var request = new MaintenanceRequest(room.Id, command.CurrentUserId, command.Description);

        dbContext.MaintenanceRequests.Add(request);
        await dbContext.SaveChangesAsync(ct);

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
}
