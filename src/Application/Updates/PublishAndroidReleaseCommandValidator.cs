using FluentValidation;

namespace Lumora.Server.Application.Updates;

public sealed class PublishAndroidReleaseCommandValidator : AbstractValidator<PublishAndroidReleaseCommand>
{
    public PublishAndroidReleaseCommandValidator()
    {
        RuleFor(x => x.Version).NotEmpty().MaximumLength(32);
        RuleFor(x => x.VersionCode).GreaterThan(0);
        RuleFor(x => x.BlobId).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.ReleaseNotes).MaximumLength(2000);
    }
}
