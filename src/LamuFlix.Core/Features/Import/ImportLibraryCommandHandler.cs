using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportLibraryCommandHandler(
    ILibraryFolderDiscovery discovery,
    IMovieFolderImporter importer,
    IOptions<LibraryOptions> libraryOptions)
    : ICommandHandler<ImportLibraryCommand, ImportLibraryResult>
{
    public async Task<ImportLibraryResult> HandleAsync(
        ImportLibraryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var maxConcurrency = libraryOptions.Value.MaxConcurrency;
        var channel = Channel.CreateBounded<LibraryPath>(new BoundedChannelOptions(maxConcurrency)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = false,
        });

        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var lifetime = shutdown.Token;
        var firstFault = new FirstFault();
        var imported = new ImportedCounter();

        var tasks = new List<Task>(maxConcurrency + 1)
        {
            GuardedAsync(
                token => ProduceAsync(channel.Writer, token),
                lifetime,
                firstFault,
                shutdown),
        };
        for (var consumer = 0; consumer < maxConcurrency; consumer++)
        {
            tasks.Add(GuardedAsync(
                token => ConsumeAsync(channel.Reader, token, imported),
                lifetime,
                firstFault,
                shutdown));
        }

        await Task.WhenAll(tasks);

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (firstFault.Value is { } failure)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return new ImportLibraryResult(imported.Value);
    }

    private async Task ProduceAsync(ChannelWriter<LibraryPath> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var folder in discovery.DiscoverAsync(cancellationToken))
            {
                await writer.WriteAsync(folder, cancellationToken);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task ConsumeAsync(
        ChannelReader<LibraryPath> reader,
        CancellationToken cancellationToken,
        ImportedCounter imported)
    {
        await foreach (var folder in reader.ReadAllAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await importer.ImportAsync(folder, cancellationToken);
            imported.Increment();
        }
    }

    private static async Task GuardedAsync(
        Func<CancellationToken, Task> body,
        CancellationToken lifetime,
        FirstFault firstFault,
        CancellationTokenSource shutdown)
    {
        try
        {
            await body(lifetime);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            // Peer shutdown or caller cancellation: the invocation reports it after every task settles.
        }
        catch (Exception exception)
        {
            firstFault.Record(exception);
            shutdown.Cancel();
        }
    }

    private sealed class FirstFault
    {
        private Exception? failure;

        public Exception? Value => Volatile.Read(ref failure);

        public void Record(Exception exception) => Interlocked.CompareExchange(ref failure, exception, null);
    }

    private sealed class ImportedCounter
    {
        private long count;

        public long Value => Interlocked.Read(ref count);

        public void Increment() => Interlocked.Increment(ref count);
    }
}
