using FluentValidation;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Application.Rooms;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(RoomSlug.MaxLength);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(128);

        When(x => x.IsPrivate, () =>
        {
            RuleFor(x => x.KdfSalt).NotNull().Must(s => s!.Length > 0)
                .WithMessage("Prywatna przestrzeń wymaga soli KDF.");
            RuleFor(x => x.AuthKey).NotNull().Must(k => k!.Length > 0)
                .WithMessage("Prywatna przestrzeń wymaga klucza uwierzytelniającego.");
        });
    }
}
