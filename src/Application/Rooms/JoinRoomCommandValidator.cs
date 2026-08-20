using FluentValidation;

namespace Lumora.Server.Application.Rooms;

public sealed class JoinRoomCommandValidator : AbstractValidator<JoinRoomCommand>
{
    public JoinRoomCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.DeviceDisplayName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(32);
    }
}
