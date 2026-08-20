using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Clipboard;

public sealed record PushClipboardEntryCommand(
    Guid RoomId,
    ClipboardEntryKind Kind,
    byte[]? InlinePayload,
    Guid? BlobId,
    int SizeBytes,
    Guid DeviceId) : IRequest<Result<ClipboardEntry>>;
