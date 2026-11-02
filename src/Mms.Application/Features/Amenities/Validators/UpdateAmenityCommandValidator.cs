using FluentValidation;
using Mms.Application.Features.Amenities.Commands;

namespace Mms.Application.Features.Amenities.Validators;

public sealed class UpdateAmenityCommandValidator : AbstractValidator<UpdateAmenityCommand>
{
    public UpdateAmenityCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên tiện nghi không được để trống.")
            .MaximumLength(100).WithMessage("Tên tiện nghi tối đa 100 ký tự.");
    }
}
