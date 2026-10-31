namespace Mms.Application.Features.Users.Queries;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20);
