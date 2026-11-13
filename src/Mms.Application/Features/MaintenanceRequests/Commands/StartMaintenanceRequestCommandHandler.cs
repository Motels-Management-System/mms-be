using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.MaintenanceRequests.Commands;

public sealed class StartMaintenanceRequestCommandHandler(
    IAppDbContext dbContext,
    IValidator<StartMaintenanceRequestCommand> validator) : ICommandHandler<StartMaintenanceRequestCommand>
{
    public async Task HandleAsync(StartMaintenanceRequestCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var request = await dbContext.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new NotFoundException("Yêu cầu bảo trì", command.Id);

        request.Start();

        await dbContext.SaveChangesAsync(ct);
    }
}
