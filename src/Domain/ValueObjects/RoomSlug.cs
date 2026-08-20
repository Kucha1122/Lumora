using System.Text.RegularExpressions;
using Lumora.Server.Domain.Common;

namespace Lumora.Server.Domain.ValueObjects;

public sealed partial class RoomSlug
{
    public const int MaxLength = 64;

    public string Value { get; }

    private RoomSlug(string value) => Value = value;

    public static Result<RoomSlug> Create(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Result<RoomSlug>.Failure("Slug nie może być pusty.");
        }

        if (candidate.Length > MaxLength)
        {
            return Result<RoomSlug>.Failure($"Slug nie może przekraczać {MaxLength} znaków.");
        }

        if (!SlugPattern().IsMatch(candidate))
        {
            return Result<RoomSlug>.Failure(
                "Slug może zawierać wyłącznie małe litery, cyfry i myślniki, bez myślnika na początku/końcu.");
        }

        return Result<RoomSlug>.Success(new RoomSlug(candidate));
    }

    public static RoomSlug Public { get; } = new("public");

    /// <summary>Reconstructs a slug already validated once (persistence round-trip only).</summary>
    internal static RoomSlug FromTrusted(string value) => new(value);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is RoomSlug other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
