using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Users.Dtos;

namespace Mms.Application.Features.Users.Queries;

public sealed class GetUsersQueryHandler(
    IIdentityService identityService,
    IValidator<GetUsersQuery> validator) : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);
        return await identityService.GetUsersAsync(query.Page, query.PageSize, ct);
    }
}
