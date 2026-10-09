using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Tests.Common;

public sealed class ManualTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private readonly List<ManualTimer> timers = [];
    private DateTimeOffset utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public override long GetTimestamp() => utcNow.UtcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state);
        timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan amount)
    {
        if (amount < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var target = utcNow + amount;
        FireDueTimers(target);
        utcNow = target;
    }

    private void FireDueTimers(DateTimeOffset target)
    {
        while (FindNextDue(target) is { } next)
        {
            utcNow = next.DueAt;
            next.Timer.Fire();
        }
    }

    private DueTimer? FindNextDue(DateTimeOffset target)
    {
        DueTimer? next = null;
        foreach (var timer in timers)
        {
            if (DueAtBy(timer, target) is not { } dueAt)
            {
                continue;
            }

            if (next is null || dueAt < next.Value.DueAt)
            {
                next = new DueTimer(timer, dueAt);
            }
        }

        return next;
    }

    private static DateTimeOffset? DueAtBy(ManualTimer timer, DateTimeOffset target)
    {
        if (timer.Disposed || timer.DueAt is not { } dueAt)
        {
            return null;
        }

        if (dueAt > target)
        {
            return null;
        }

        return dueAt;
    }

    private readonly record struct DueTimer(ManualTimer Timer, DateTimeOffset DueAt);

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan period = Timeout.InfiniteTimeSpan;

        public DateTimeOffset? DueAt { get; private set; }

        public bool Disposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan newPeriod)
        {
            if (Disposed)
            {
                return false;
            }

            period = newPeriod;
            DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner.utcNow + dueTime;
            return true;
        }

        public void Dispose()
        {
            Disposed = true;
            DueAt = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Fire()
        {
            if (period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero)
            {
                DueAt = null;
            }
            else
            {
                DueAt = owner.utcNow + period;
            }

            callback(state);
        }
    }
}