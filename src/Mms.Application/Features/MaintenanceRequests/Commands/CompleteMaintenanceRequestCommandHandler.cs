using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.MaintenanceRequests.Commands;

public sealed class CompleteMaintenanceRequestCommandHandler(
    IAppDbContext dbContext,
    IValidator<CompleteMaintenanceRequestCommand> validator) : ICommandHandler<CompleteMaintenanceRequestCommand>
{
    public async Task HandleAsync(CompleteMaintenanceRequestCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var request = await dbContext.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new NotFoundException("Yêu cầu bảo trì", command.Id);

        request.Complete();

        await dbContext.SaveChangesAsync(ct);
    }
}
