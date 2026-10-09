using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace LamuFlix.UnitTests.Enrichment;

public sealed class StrandedMovieSweeperTests
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task ExecuteAsync_RunsImmediatelyThenWaitsFullIntervalAfterSuccess()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var handler = Substitute.For<ICommandHandler<SweepStrandedMoviesCommand, int>>();
        var firstPass = NewSignal();
        var secondPass = NewSignal();
        var scopeDisposed = NewSignal();
        var calls = 0;
        handler.HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                (Interlocked.Increment(ref calls) == 1 ? firstPass : secondPass).TrySetResult();
                return Task.FromResult(0);
            });
        await using var provider = CreateProvider(timeProvider, handler, scopeDisposed);
        var sweeper = new StrandedMovieSweeper(provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { SweepInterval = SweepInterval }),
            timeProvider, NullLogger<StrandedMovieSweeper>.Instance);

        var execution = sweeper.StartAsync(TestContext.Current.CancellationToken);
        await firstPass.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await scopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Task.Yield();
        await handler.Received(1).HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>());

        timeProvider.Advance(SweepInterval - TimeSpan.FromTicks(1));
        await Task.Yield();
        await handler.Received(1).HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>());
        timeProvider.Advance(TimeSpan.FromTicks(1));
        await secondPass.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await handler.Received(2).HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>());

        await sweeper.StopAsync(TestContext.Current.CancellationToken);
        await execution;
        sweeper.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_FailureIsLoggedAndLaterPassRunsAfterInterval()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var handler = Substitute.For<ICommandHandler<SweepStrandedMoviesCommand, int>>();
        var firstPass = NewSignal();
        var laterPass = NewSignal();
        var failure = new InvalidOperationException("sweep failed");
        var call = 0;
        handler.HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref call) == 1
                ? FailAfterSignal(firstPass, failure)
                : SignalSuccess(laterPass));
        var logger = new RecordingLogger<StrandedMovieSweeper>();
        await using var provider = CreateProvider(timeProvider, handler);
        var sweeper = new StrandedMovieSweeper(provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { SweepInterval = SweepInterval }),
            timeProvider, logger);

        await sweeper.StartAsync(TestContext.Current.CancellationToken);
        await firstPass.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await logger.ErrorLogged.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Error && entry.Exception is InvalidOperationException);
        timeProvider.Advance(SweepInterval);
        await laterPass.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await sweeper.StopAsync(TestContext.Current.CancellationToken);
        sweeper.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_CancelsAnActivePassDuringShutdown()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var handler = Substitute.For<ICommandHandler<SweepStrandedMoviesCommand, int>>();
        var started = NewSignal();
        var canceled = NewSignal();
        handler.HandleAsync(Arg.Any<SweepStrandedMoviesCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => ObserveCancellation(call.Arg<CancellationToken>(), started, canceled));
        await using var provider = CreateProvider(timeProvider, handler);
        var sweeper = new StrandedMovieSweeper(provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { SweepInterval = SweepInterval }),
            timeProvider, NullLogger<StrandedMovieSweeper>.Instance);

        await sweeper.StartAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await sweeper.StopAsync(TestContext.Current.CancellationToken);
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        sweeper.Dispose();
    }

    private static ServiceProvider CreateProvider(TimeProvider timeProvider,
        ICommandHandler<SweepStrandedMoviesCommand, int> handler, TaskCompletionSource? scopeDisposed = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(timeProvider)
            .AddScoped(_ => new ScopeProbe(scopeDisposed))
            .AddScoped<ICommandHandler<SweepStrandedMoviesCommand, int>>(serviceProvider =>
            {
                _ = serviceProvider.GetRequiredService<ScopeProbe>();
                return handler;
            });
        return services.BuildServiceProvider();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Task<int> FailAfterSignal(TaskCompletionSource signal, Exception exception)
    {
        signal.TrySetResult();
        return Task.FromException<int>(exception);
    }

    private static Task<int> SignalSuccess(TaskCompletionSource signal)
    {
        signal.TrySetResult();
        return Task.FromResult(0);
    }

    private static async Task<int> ObserveCancellation(CancellationToken cancellationToken,
        TaskCompletionSource started, TaskCompletionSource canceled)
    {
        started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            canceled.TrySetResult();
            throw;
        }

        return 0;
    }


    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public ConcurrentBag<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public TaskCompletionSource ErrorLogged { get; } = NewSignal();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception));
            if (logLevel == LogLevel.Error)
            {
                ErrorLogged.TrySetResult();
            }
        }
    }

    private sealed class ScopeProbe(TaskCompletionSource? disposed) : IDisposable
    {
        public void Dispose() => disposed?.TrySetResult();
    }
}