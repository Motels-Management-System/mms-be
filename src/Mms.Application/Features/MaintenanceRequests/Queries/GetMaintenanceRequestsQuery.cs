using Mms.Domain.Enums;

namespace Mms.Application.Features.MaintenanceRequests.Queries;

public sealed record GetMaintenanceRequestsQuery(
    MaintenanceStatus? Status = null,
    Guid? RoomId = null,
    int Page = 1,
    int PageSize = 20);
