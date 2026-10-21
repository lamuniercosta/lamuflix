using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.UnitTests;

// Thread-safe: code under test creates and changes timers on its own threads while the test thread
// advances the clock. Timer callbacks run outside the lock so they may create or change timers.
public sealed class ManualTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private readonly Lock gate = new();
    private readonly List<ManualTimer> timers = [];
    private readonly List<TaskCompletionSource> pendingTimerWaiters = [];
    private DateTimeOffset utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow()
    {
        lock (gate)
        {
            return utcNow;
        }
    }

    public override long GetTimestamp() => GetUtcNow().UtcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public bool HasPendingTimer
    {
        get
        {
            lock (gate)
            {
                return HasPendingTimerCore();
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state);
        lock (gate)
        {
            timers.Add(timer);
        }

        timer.Change(dueTime, period);
        return timer;
    }

    // Completes once a timer is scheduled to fire, so a test can advance the clock knowing the
    // code under test has already armed its delay rather than racing it.
    public Task WaitForPendingTimerAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (HasPendingTimerCore())
            {
                return Task.CompletedTask;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            pendingTimerWaiters.Add(waiter);
            return waiter.Task.WaitAsync(cancellationToken);
        }
    }

    public void Advance(TimeSpan amount)
    {
        if (amount < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        DateTimeOffset target;
        lock (gate)
        {
            target = utcNow + amount;
        }

        FireDueTimers(target);
        lock (gate)
        {
            utcNow = target;
        }
    }

    private void FireDueTimers(DateTimeOffset target)
    {
        while (TakeNextDue(target) is { } next)
        {
            next.Fire();
        }
    }

    private ManualTimer? TakeNextDue(DateTimeOffset target)
    {
        lock (gate)
        {
            timers.RemoveAll(timer => timer.Disposed);
            if (FindEarliestDue(target) is not { } next)
            {
                return null;
            }

            utcNow = next.DueAt;
            next.Timer.Reschedule();
            return next.Timer;
        }
    }

    private DueTimer? FindEarliestDue(DateTimeOffset target)
    {
        DueTimer? next = null;
        foreach (var timer in timers)
        {
            if (timer.DueAt is { } dueAt && dueAt <= target && (next is null || dueAt < next.Value.DueAt))
            {
                next = new DueTimer(timer, dueAt);
            }
        }

        return next;
    }

    private bool HasPendingTimerCore() => timers.Exists(timer => timer.DueAt is not null);

    private void Schedule(ManualTimer timer, DateTimeOffset? dueAt)
    {
        lock (gate)
        {
            timer.DueAt = timer.Disposed ? null : dueAt;
            if (timer.DueAt is null)
            {
                return;
            }

            foreach (var waiter in pendingTimerWaiters)
            {
                waiter.TrySetResult();
            }

            pendingTimerWaiters.Clear();
        }
    }

    private readonly record struct DueTimer(ManualTimer Timer, DateTimeOffset DueAt);

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan period = Timeout.InfiniteTimeSpan;

        // Guarded by owner.gate.
        public DateTimeOffset? DueAt { get; set; }

        public bool Disposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan newPeriod)
        {
            lock (owner.gate)
            {
                if (Disposed)
                {
                    return false;
                }

                period = newPeriod;
                owner.Schedule(this, dueTime == Timeout.InfiniteTimeSpan ? null : owner.utcNow + dueTime);
                return true;
            }
        }

        public void Dispose()
        {
            lock (owner.gate)
            {
                Disposed = true;
                DueAt = null;
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        // Called under owner.gate with owner.utcNow set to this timer's due time.
        public void Reschedule()
        {
            var oneShot = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero;
            owner.Schedule(this, oneShot ? null : owner.utcNow + period);
        }

        public void Fire() => callback(state);
    }
}
