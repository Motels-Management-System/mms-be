using FluentValidation;
using Mms.Application.Features.Dashboard.Queries;

namespace Mms.Application.Features.Dashboard.Validators;

public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        // Query không có tham số nào cần kiểm tra.
    }
}
