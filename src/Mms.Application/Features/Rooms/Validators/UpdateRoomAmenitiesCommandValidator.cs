using FluentValidation;
using Mms.Application.Features.Rooms.Commands;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class UpdateRoomAmenitiesCommandValidator : AbstractValidator<UpdateRoomAmenitiesCommand>
{
    public UpdateRoomAmenitiesCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Mã phòng không được để trống.");

        RuleFor(x => x.AmenityIds)
            .NotNull().WithMessage("Danh sách tiện nghi không được để trống.");
    }
}
