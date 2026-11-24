namespace CoilPro.Commands.Tests.Protocol.TestSupport;

/// <summary>A <see cref="TimeProvider"/> whose clock only moves when told to.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public ManualTimeProvider()
        : this(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;

    public void SetUtcNow(DateTimeOffset now) => _now = now;
}
