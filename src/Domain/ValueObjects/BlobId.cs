namespace Lumora.Server.Domain.ValueObjects;

public sealed class BlobId
{
    public Guid Value { get; }

    private BlobId(Guid value) => Value = value;

    public static BlobId New() => new(Guid.NewGuid());

    public static BlobId From(Guid value) => new(value);

    public override string ToString() => Value.ToString("N");

    public override bool Equals(object? obj) => obj is BlobId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
