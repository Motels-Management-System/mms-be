using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.Amenities.Commands;

public sealed class DeleteAmenityCommandHandler(
    IAppDbContext dbContext,
    IValidator<DeleteAmenityCommand> validator) : ICommandHandler<DeleteAmenityCommand>
{
    public async Task HandleAsync(DeleteAmenityCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var amenity = await dbContext.Amenities
            .FirstOrDefaultAsync(a => a.Id == command.Id, ct);

        if (amenity is null)
            throw new NotFoundException("Tiện nghi", command.Id);

        var isAssignedToRoom = await dbContext.RoomAmenities
            .AnyAsync(ra => ra.AmenityId == command.Id, ct);

        if (isAssignedToRoom)
            throw new DomainException("Không thể xóa tiện nghi đang được gán cho phòng.");

        dbContext.Amenities.Remove(amenity);
        await dbContext.SaveChangesAsync(ct);
    }
}
