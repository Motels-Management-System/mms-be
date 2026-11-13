using FluentValidation;
using Mms.Application.Features.MeterReadings.Queries;

namespace Mms.Application.Features.MeterReadings.Validators;

public sealed class GetMeterReadingsQueryValidator : AbstractValidator<GetMeterReadingsQuery>
{
    public GetMeterReadingsQueryValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Phòng là bắt buộc.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
