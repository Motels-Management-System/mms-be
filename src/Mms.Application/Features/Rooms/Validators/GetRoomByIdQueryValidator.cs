using FluentValidation;
using Mms.Application.Features.Rooms.Queries;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class GetRoomByIdQueryValidator : AbstractValidator<GetRoomByIdQuery>
{
    public GetRoomByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");
    }
}
