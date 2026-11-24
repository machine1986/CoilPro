// Port of teslamotors/vehicle-command internal/authentication/window.go (Apache-2.0).

namespace CoilPro.Commands.Internal.Crypto;

/// <summary>
/// Anti-replay window over 32-bit counters: accepts each counter value at most once, tolerating
/// out-of-order arrival within <see cref="WindowSize"/> values of the highest counter seen.
/// </summary>
internal sealed class SlidingWindow
{
    /// <summary>How far back (in counter values) out-of-order messages are still accepted.</summary>
    public const uint WindowSize = 32;

    private ulong _history;
    private uint _counter;
    private bool _used;

    /// <summary>Records <paramref name="counter"/>; false if it was seen before (or is too old to tell).</summary>
    public bool Update(uint counter)
    {
        if (!_used)
        {
            _used = true;
            _counter = counter;
            return true;
        }

        return TryUpdate(ref _counter, ref _history, counter);
    }

    /// <summary>
    /// Updates <paramref name="counter"/> (the highest authentic counter so far) and its <paramref name="window"/>
    /// with <paramref name="newCounter"/>. Returns false, leaving both unchanged, if <paramref name="newCounter"/>
    /// may have been used before.
    /// </summary>
    public static bool TryUpdate(ref uint counter, ref ulong window, uint newCounter)
    {
        if (counter == newCounter)
        {
            return false;
        }

        if (newCounter < counter)
        {
            // Arrived out of order.
            var age = counter - newCounter;
            if (age > WindowSize)
            {
                return false;
            }

            if (((window >> (int)(age - 1)) & 1) == 1)
            {
                return false;
            }

            window |= 1UL << (int)(age - 1);
            return true;
        }

        var shift = newCounter - counter;
        // Unlike Go, C# masks shift counts to 6 bits, so shifts of 64 or more must be handled explicitly.
        window = shift >= 64 ? 0 : window << (int)shift;
        if (shift - 1 < 64)
        {
            window |= 1UL << (int)(shift - 1);
        }

        counter = newCounter;
        return true;
    }
}
