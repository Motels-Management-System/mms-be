using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Amenities.Commands;

public sealed class CreateAmenityCommandHandler(
    IAppDbContext dbContext,
    IValidator<CreateAmenityCommand> validator) : ICommandHandler<CreateAmenityCommand, AmenityDto>
{
    public async Task<AmenityDto> HandleAsync(CreateAmenityCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var trimmedName = command.Name.Trim();
        var exists = await dbContext.Amenities
            .AnyAsync(a => a.Name.ToLower() == trimmedName.ToLower(), ct);

        if (exists)
            throw new ConflictException("Tên tiện nghi đã tồn tại.");

        var amenity = new Amenity(command.Name);
        dbContext.Amenities.Add(amenity);
        await dbContext.SaveChangesAsync(ct);

        return new AmenityDto(amenity.Id, amenity.Name, amenity.CreatedAt, amenity.UpdatedAt);
    }
}
