using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Clipboard;
using MediatR;

namespace Lumora.Server.Application.Clipboard;

public sealed record ListClipboardEntriesQuery(Guid RoomId, int Take = 100)
    : IRequest<IReadOnlyList<ClipboardEntry>>;

public sealed class ListClipboardEntriesHandler(IClipboardRepository clipboard)
    : IRequestHandler<ListClipboardEntriesQuery, IReadOnlyList<ClipboardEntry>>
{
    public Task<IReadOnlyList<ClipboardEntry>> Handle(ListClipboardEntriesQuery request, CancellationToken ct) =>
        clipboard.ListRecentAsync(request.RoomId, request.Take, ct);
}
