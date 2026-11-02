using FluentValidation;
using Mms.Application.Features.Rooms.Commands;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class ChangeRoomStatusCommandValidator : AbstractValidator<ChangeRoomStatusCommand>
{
    public ChangeRoomStatusCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Mã phòng không được để trống.");

        RuleFor(x => x.Status)
            .Must(s => s is RoomStatus.Available or RoomStatus.Maintenance)
            .WithMessage("Chỉ được chuyển trạng thái giữa Có sẵn (Available) và Đang bảo trì (Maintenance).");
    }
}
