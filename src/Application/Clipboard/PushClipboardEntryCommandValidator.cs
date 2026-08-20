using FluentValidation;

namespace Lumora.Server.Application.Clipboard;

public sealed class PushClipboardEntryCommandValidator : AbstractValidator<PushClipboardEntryCommand>
{
    public PushClipboardEntryCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.InlinePayload is not null || x.BlobId is not null)
            .WithMessage("Wpis musi mieć payload inline albo referencję do bloba.");
    }
}
