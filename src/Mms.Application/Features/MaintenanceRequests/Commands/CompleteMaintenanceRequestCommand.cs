namespace Mms.Application.Features.MaintenanceRequests.Commands;

/// <summary>Nhân viên hoàn tất yêu cầu bảo trì (InProgress → Done).</summary>
public sealed record CompleteMaintenanceRequestCommand(Guid Id);
