using FluentValidation;
using Mms.Application.Features.Amenities.Commands;

namespace Mms.Application.Features.Amenities.Validators;

public sealed class CreateAmenityCommandValidator : AbstractValidator<CreateAmenityCommand>
{
    public CreateAmenityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên tiện nghi không được để trống.")
            .MaximumLength(100).WithMessage("Tên tiện nghi tối đa 100 ký tự.");
    }
}
