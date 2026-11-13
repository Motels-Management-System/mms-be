using FluentValidation;
using Mms.Application.Features.MeterReadings.Commands;

namespace Mms.Application.Features.MeterReadings.Validators;

public sealed class CreateMeterReadingCommandValidator : AbstractValidator<CreateMeterReadingCommand>
{
    public CreateMeterReadingCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Phòng là bắt buộc.");

        RuleFor(x => x.Electricity)
            .GreaterThanOrEqualTo(0m).WithMessage("Chỉ số điện không được âm.")
            .LessThan(100_000_000_000m).WithMessage("Chỉ số điện vượt ngoài phạm vi lưu trữ.");

        RuleFor(x => x.Water)
            .GreaterThanOrEqualTo(0m).WithMessage("Chỉ số nước không được âm.")
            .LessThan(100_000_000_000m).WithMessage("Chỉ số nước vượt ngoài phạm vi lưu trữ.");
    }
}
