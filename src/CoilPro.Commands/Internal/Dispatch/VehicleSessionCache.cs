using System.Collections.Concurrent;
using CoilPro.Commands.Internal.Protocol;
using UniversalMessage;

namespace CoilPro.Commands.Internal.Dispatch;

/// <summary>
/// Per-VIN command gates (commands to one vehicle must be strictly ordered, VCSEC counters) and the cache of
/// established sessions keyed by (VIN, domain).
/// </summary>
internal sealed class VehicleSessionCache
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string Vin, Domain Domain), VehicleSession> _sessions = new();

    /// <summary>Waits for exclusive access to <paramref name="vin"/>; dispose the result to release it.</summary>
    public async Task<IDisposable> LockAsync(string vin, CancellationToken ct)
    {
        var gate = _gates.GetOrAdd(vin, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(gate);
    }

    /// <summary>Returns the cached session for (<paramref name="vin"/>, <paramref name="domain"/>), or null.</summary>
    public VehicleSession? Get(string vin, Domain domain) =>
        _sessions.TryGetValue((vin, domain), out var session) ? session : null;

    /// <summary>Caches <paramref name="session"/> under its own domain.</summary>
    public void Set(string vin, VehicleSession session) => _sessions[(vin, session.Domain)] = session;

    /// <summary>Removes <paramref name="session"/> if it is still the cached one.</summary>
    public void Drop(string vin, VehicleSession session) =>
        _sessions.TryRemove(new KeyValuePair<(string, Domain), VehicleSession>((vin, session.Domain), session));

    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
