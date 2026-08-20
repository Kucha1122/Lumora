using Lumora.Server.Application.Abstractions;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Lumora.Server.Infrastructure.Storage;

/// <summary>
/// Stores opaque, already-encrypted blobs on a mounted volume (a PVC in k3s).
/// Paths are built exclusively from GUIDs, so there is no path-traversal surface.
/// </summary>
public sealed class LocalFileSystemBlobStore(IOptions<BlobStoreOptions> options) : IBlobStore
{
    private readonly string root = options.Value.RootPath;

    public async Task<long> SaveAsync(Guid roomId, BlobId blobId, Stream content, CancellationToken ct)
    {
        var path = GetPath(roomId, blobId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
        return file.Length;
    }

    public Task<Stream> OpenReadAsync(Guid roomId, BlobId blobId, CancellationToken ct)
    {
        var path = GetPath(roomId, blobId);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(Guid roomId, BlobId blobId, CancellationToken ct)
    {
        var path = GetPath(roomId, blobId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(Guid roomId, BlobId blobId) =>
        Path.Combine(root, roomId.ToString("N"), blobId.ToString());
}
