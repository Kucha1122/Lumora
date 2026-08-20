using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Application.Abstractions;

/// <summary>
/// Stores opaque, already-encrypted blobs on behalf of a room. The application layer
/// never inspects blob content — only size and identity.
/// </summary>
public interface IBlobStore
{
    Task<long> SaveAsync(Guid roomId, BlobId blobId, Stream content, CancellationToken ct);

    Task<Stream> OpenReadAsync(Guid roomId, BlobId blobId, CancellationToken ct);

    Task DeleteAsync(Guid roomId, BlobId blobId, CancellationToken ct);
}
