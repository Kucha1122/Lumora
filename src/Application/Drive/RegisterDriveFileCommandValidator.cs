using FluentValidation;

namespace Lumora.Server.Application.Drive;

public sealed class RegisterDriveFileCommandValidator : AbstractValidator<RegisterDriveFileCommand>
{
    public RegisterDriveFileCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.BlobId).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.EncryptedMetadata).NotNull().Must(m => m.Length > 0);
    }
}
