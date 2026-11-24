namespace CoilPro.Commands.Tests.Orchestration;

/// <summary>
/// Minimal manually advanced <see cref="TimeProvider"/> (the test project does not reference
/// Microsoft.Extensions.TimeProvider.Testing). Timers fire only inside <see cref="Advance"/>.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _lock = new();
    private readonly List<FakeTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _now;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state);
        timer.Change(dueTime, period);
        lock (_lock)
        {
            _timers.Add(timer);
        }

        return timer;
    }

    /// <summary>Moves the clock forward, firing due timers in order.</summary>
    public void Advance(TimeSpan by)
    {
        DateTimeOffset target;
        lock (_lock)
        {
            target = _now + by;
        }

        while (true)
        {
            FakeTimer? next;
            lock (_lock)
            {
                next = _timers.Where(t => t.Due is { } d && d <= target).OrderBy(t => t.Due).FirstOrDefault();
                if (next is null)
                {
                    _now = target;
                    return;
                }

                if (next.Due > _now)
                {
                    _now = next.Due!.Value;
                }

                next.Due = null;
            }

            next.Fire();
        }
    }

    /// <summary>Advances the clock in small steps until <paramref name="task"/> completes (or the budget runs out).</summary>
    public async Task PumpUntilCompletedAsync(Task task, TimeSpan step = default, int maxSteps = 5000)
    {
        step = step == default ? TimeSpan.FromMilliseconds(500) : step;
        for (var i = 0; i < maxSteps && !task.IsCompleted; i++)
        {
            Advance(step);
            await Task.Delay(1);
        }

        Assert.True(task.IsCompleted, "task did not complete while the fake clock was advancing");
    }

    private void Remove(FakeTimer timer)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class FakeTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public DateTimeOffset? Due { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner._lock)
            {
                Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime;
            }

            return true;
        }

        public void Fire() => callback(state);

        public void Dispose()
        {
            Due = null;
            owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
