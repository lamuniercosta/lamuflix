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

    public Task Track(Task work)
    {
        ArgumentNullException.ThrowIfNull(work);
        lock (gate)
        {
            if (count == 0)
            {
                idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            count++;
        }

        return ObserveAsync(work);
    }

    private async Task ObserveAsync(Task work)
    {
        try
        {
            await work;
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

    public Task WhenIdleAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return count == 0 ? Task.CompletedTask : idle.Task.WaitAsync(cancellationToken);
        }
    }

    private static TaskCompletionSource Completed()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        completed.TrySetResult();
        return completed;
    }
}
