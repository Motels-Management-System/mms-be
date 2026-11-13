namespace Mms.Application.Features.MaintenanceRequests.Queries;

public sealed record GetMaintenanceRequestByIdQuery(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
