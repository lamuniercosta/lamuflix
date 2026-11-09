using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class ConsumerDrain
{
    private readonly object gate = new();
    private int count;
    private TaskCompletionSource idle = Completed();

    // Takes the work unstarted and counts it before starting it. A handler can run synchronously
    // for a while before it returns its task; counting only once that task exists let a shutdown
    // in that window see nothing in flight and skip the drain.
    public Task Track(Func<Task> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        lock (gate)
        {
            if (count == 0)
            {
                idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            count++;
        }

        return ObserveAsync(start);
    }

    private async Task ObserveAsync(Func<Task> start)
    {
        try
        {
            await start();
        }
        finally
        {
            lock (gate)
            {
                count--;
                if (count == 0)
                {
                    idle.TrySetResult();
                }
            }
        }
    }

    public async Task PauseAndDrainAsync(IChannel channel, string consumerTag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        try
        {
            if (channel.IsOpen)
            {
                await channel.BasicCancelAsync(consumerTag, false, cancellationToken);
            }
        }
        finally
        {
            await WhenIdleAsync(cancellationToken);
        }
    }

    // idle is complete whenever nothing is in flight, so it alone answers the question.
    public Task WhenIdleAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return idle.Task.WaitAsync(cancellationToken);
        }
    }

    private static TaskCompletionSource Completed()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        completed.TrySetResult();
        return completed;
    }
}
