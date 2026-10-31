using FluentValidation;
using Mms.Application.Features.Users.Queries;

namespace Mms.Application.Features.Users.Validators;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Số mục trên trang phải từ 1 đến 100.");
    }
}
