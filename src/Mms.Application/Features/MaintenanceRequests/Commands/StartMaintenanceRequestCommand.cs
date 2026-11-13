namespace Mms.Application.Features.MaintenanceRequests.Commands;

/// <summary>Nhân viên bắt đầu xử lý yêu cầu bảo trì (Open → InProgress).</summary>
public sealed record StartMaintenanceRequestCommand(Guid Id);
