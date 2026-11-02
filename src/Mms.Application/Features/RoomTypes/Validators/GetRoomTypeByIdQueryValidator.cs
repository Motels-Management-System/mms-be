using FluentValidation;
using Mms.Application.Features.RoomTypes.Queries;

namespace Mms.Application.Features.RoomTypes.Validators;

public sealed class GetRoomTypeByIdQueryValidator : AbstractValidator<GetRoomTypeByIdQuery>
{
    public GetRoomTypeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");
    }
}
