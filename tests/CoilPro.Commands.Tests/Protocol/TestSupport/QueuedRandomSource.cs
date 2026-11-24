using CoilPro.Commands.Internal;

namespace CoilPro.Commands.Tests.Protocol.TestSupport;

/// <summary>
/// Deterministic <see cref="IRandomSource"/>: serves queued byte strings in order (each must match the requested
/// length exactly), then falls back to a simple counter pattern so tests never depend on real randomness.
/// </summary>
internal sealed class QueuedRandomSource : IRandomSource
{
    private readonly Queue<byte[]> _queue = new();
    private byte _next = 1;

    public QueuedRandomSource(params string[] hexValues)
    {
        foreach (var hex in hexValues)
        {
            Enqueue(hex);
        }
    }

    public QueuedRandomSource Enqueue(string hex)
    {
        _queue.Enqueue(Convert.FromHexString(hex));
        return this;
    }

    public void Fill(Span<byte> buffer)
    {
        if (_queue.TryDequeue(out var value))
        {
            if (value.Length != buffer.Length)
            {
                throw new InvalidOperationException($"Queued random value has {value.Length} bytes, but {buffer.Length} were requested.");
            }

            value.CopyTo(buffer);
            return;
        }

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = _next++;
        }
    }
}
