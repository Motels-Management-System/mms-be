using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;

namespace Mms.Application.Features.Amenities.Commands;

public sealed class UpdateAmenityCommandHandler(
    IAppDbContext dbContext,
    IValidator<UpdateAmenityCommand> validator) : ICommandHandler<UpdateAmenityCommand, AmenityDto>
{
    public async Task<AmenityDto> HandleAsync(UpdateAmenityCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var amenity = await dbContext.Amenities
            .FirstOrDefaultAsync(a => a.Id == command.Id, ct);

        if (amenity is null)
            throw new NotFoundException("Tiện nghi", command.Id);

        var trimmedName = command.Name.Trim();
        var duplicate = await dbContext.Amenities
            .AnyAsync(a => a.Id != command.Id && a.Name.ToLower() == trimmedName.ToLower(), ct);

        if (duplicate)
            throw new ConflictException("Tên tiện nghi đã tồn tại.");

        amenity.Update(command.Name);
        await dbContext.SaveChangesAsync(ct);

        return new AmenityDto(amenity.Id, amenity.Name, amenity.CreatedAt, amenity.UpdatedAt);
    }
}
