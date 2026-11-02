using FluentValidation;
using Mms.Application.Features.Amenities.Commands;

namespace Mms.Application.Features.Amenities.Validators;

public sealed class DeleteAmenityCommandValidator : AbstractValidator<DeleteAmenityCommand>
{
    public DeleteAmenityCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");
    }
}
