using System.Security.Cryptography;

namespace CoilPro.Commands.Internal;

/// <summary>
/// Source of random bytes (nonces, message UUIDs, routing addresses). Injectable so tests can produce
/// byte-exact messages that match Tesla's published test vectors.
/// </summary>
internal interface IRandomSource
{
    /// <summary>Fills <paramref name="buffer"/> with random bytes.</summary>
    void Fill(Span<byte> buffer);
}

/// <summary>Cryptographically secure <see cref="IRandomSource"/> backed by <see cref="RandomNumberGenerator"/>.</summary>
internal sealed class SystemRandomSource : IRandomSource
{
    public static SystemRandomSource Instance { get; } = new();

    public void Fill(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);
}
