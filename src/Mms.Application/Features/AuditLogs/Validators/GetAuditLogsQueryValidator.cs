using FluentValidation;
using Mms.Application.Features.AuditLogs.Queries;

namespace Mms.Application.Features.AuditLogs.Validators;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.EntityName)
            .MaximumLength(100).WithMessage("Tên thực thể không được vượt quá 100 ký tự.");

        RuleFor(x => x.EntityId)
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("Id bản ghi phải là GUID.")
            .When(x => !string.IsNullOrWhiteSpace(x.EntityId));

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("Ngày kết thúc phải không nhỏ hơn ngày bắt đầu.")
            .When(x => x.From.HasValue && x.To.HasValue);

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
