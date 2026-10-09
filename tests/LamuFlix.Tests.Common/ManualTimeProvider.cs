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
        while (true)
        {
            ManualTimer? next = null;
            foreach (var timer in timers)
            {
                if (!timer.Disposed && timer.DueAt is { } dueAt && dueAt <= target
                    && (next is null || dueAt < next.DueAt))
                {
                    next = timer;
                }
            }

            if (next is null)
            {
                break;
            }

            utcNow = next.DueAt!.Value;
            next.Fire();
        }

        utcNow = target;
    }

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