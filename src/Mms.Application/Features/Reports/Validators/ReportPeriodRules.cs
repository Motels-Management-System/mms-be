using FluentValidation;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Application.Features.Reports.Validators;

/// <summary>
/// Quy tắc khoảng thời gian cho báo cáo: [From, To) và giới hạn độ dài để generate_series
/// không phải sinh quá nhiều dòng cho một request.
/// </summary>
public static class ReportPeriodRules
{
    public const int MaxPeriodDays = 366;

    public static void ValidatePeriod<T>(this AbstractValidator<T> validator)
        where T : IReportPeriodQuery
    {
        validator.RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber <= MaxPeriodDays)
            .WithMessage($"Khoảng báo cáo tối đa {MaxPeriodDays} ngày.");
    }
}
