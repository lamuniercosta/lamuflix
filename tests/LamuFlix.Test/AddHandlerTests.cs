using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Pipeline;
using ValidationException = LamuFlix.Core.Pipeline.ValidationException;
using LamuFlix.Infrastructure.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace LamuFlix.Test;

public class AddHandlerTests
{
    [Fact]
    public async Task AddHandler_Success_RecordsDecoratorOrderAndHandlerSpan()
    {
        // arrange
        var sequence = new List<string>();
        var logger = new CapturingLogger(sequence);
        using var listener = Listen(sequence);
        await using var provider = BuildProvider<PingHandler, Ping, string>(
            new CapturingLoggerFactory(logger),
            new FixedTimeProvider(),
            new RecordingValidator(sequence));
        var handler = provider.GetRequiredService<ICommandHandler<Ping, string>>();
        PingHandler.Sequence = sequence;

        // act
        string result;
        try
        {
            result = await handler.HandleAsync(new Ping("ok"), CancellationToken.None);
        }
        finally
        {
            PingHandler.Sequence = null;
        }

        // assert
        result.ShouldBe("ok");
        handler.ShouldBeOfType<TracingDecorator<Ping, string>>();
        sequence.ShouldBe(["Tracing:start", "Validation", "Handler", "Logging:complete", "Tracing:stop"]);
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("42");
        entry.Activity.ShouldNotBeNull();
        entry.Activity.Source.Name.ShouldBe(TelemetryConstants.ActivitySourceName);
        entry.Activity.DisplayName.ShouldBe(nameof(Ping));
    }

    [Fact]
    public async Task AddHandler_FailingValidator_LogsWarningAndSkipsHandler()
    {
        // arrange
        var logger = new CapturingLogger([]);
        await using var provider = BuildProvider<CountingHandler, Ping, string>(
            new CapturingLoggerFactory(logger),
            TimeProvider.System,
            new RejectingValidator());
        var handler = provider.GetRequiredService<ICommandHandler<Ping, string>>();
        CountingHandler.Calls = 0;

        // act
        await Should.ThrowAsync<ValidationException>(
            () => handler.HandleAsync(new Ping("x"), CancellationToken.None));

        // assert
        CountingHandler.Calls.ShouldBe(0);
        logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task AddHandler_QueryHandler_ResolvesTracingDecorator()
    {
        // arrange
        await using var provider = BuildProvider<PingQueryHandler, Ping, string>(
            new CapturingLoggerFactory(new CapturingLogger([])),
            TimeProvider.System);

        // act
        var handler = provider.GetRequiredService<IQueryHandler<Ping, string>>();
        var result = await handler.HandleAsync(new Ping("q"), CancellationToken.None);

        // assert
        handler.ShouldBeOfType<TracingDecorator<Ping, string>>();
        result.ShouldBe("q");
    }

    [Fact]
    public async Task AddHandler_HandlerImplementingBoth_ResolvesEachInterface()
    {
        // arrange
        await using var provider = BuildProvider<BothHandler, Ping, string>(
            new CapturingLoggerFactory(new CapturingLogger([])),
            TimeProvider.System);

        // act
        var command = await provider.GetRequiredService<ICommandHandler<Ping, string>>()
            .HandleAsync(new Ping("both"), CancellationToken.None);
        var query = await provider.GetRequiredService<IQueryHandler<Ping, string>>()
            .HandleAsync(new Ping("both"), CancellationToken.None);

        // assert
        command.ShouldBe("command");
        query.ShouldBe("query");
    }

    [Fact]
    public void AddHandler_NeitherInterface_ThrowsInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => services.AddHandler<NotAHandler, Ping, string>());

        // assert
        exception.Message.ShouldContain(nameof(NotAHandler));
    }

    [Fact]
    public async Task AddHandler_ManualValidator_IsAppliedAndUnregisteredValidatorIsNot()
    {
        // arrange
        var ran = false;
        await using var provider = BuildProvider<PingHandler, Ping, string>(
            new CapturingLoggerFactory(new CapturingLogger([])),
            TimeProvider.System,
            new CallbackValidator(() => ran = true));
        var handler = provider.GetRequiredService<ICommandHandler<Ping, string>>();

        // act
        var result = await handler.HandleAsync(new Ping("kept"), CancellationToken.None);

        // assert
        ran.ShouldBeTrue();
        result.ShouldBe("kept");
    }

    private static ServiceProvider BuildProvider<THandler, TReq, TRes>(
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider,
        params IValidator<TReq>[] validators)
        where THandler : class, new()
        where TReq : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(timeProvider);
        foreach (var validator in validators)
        {
            services.AddSingleton(validator);
        }

        services.AddHandler<THandler, TReq, TRes>();
        return services.BuildServiceProvider();
    }

    private static ActivityListener Listen(List<string> sequence)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => sequence.Add("Tracing:start"),
            ActivityStopped = _ => sequence.Add("Tracing:stop"),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    public sealed record Ping(string Value);

    private sealed class PingHandler : ICommandHandler<Ping, string>
    {
        public static List<string>? Sequence { get; set; }

        public Task<string> HandleAsync(Ping command, CancellationToken cancellationToken)
        {
            Sequence?.Add("Handler");
            return Task.FromResult(command.Value);
        }
    }

    private sealed class CountingHandler : ICommandHandler<Ping, string>
    {
        public static int Calls { get; set; }

        public Task<string> HandleAsync(Ping command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(command.Value);
        }
    }

    private sealed class PingQueryHandler : IQueryHandler<Ping, string>
    {
        public Task<string> HandleAsync(Ping query, CancellationToken cancellationToken) =>
            Task.FromResult(query.Value);
    }

    private sealed class BothHandler : ICommandHandler<Ping, string>, IQueryHandler<Ping, string>
    {
        Task<string> ICommandHandler<Ping, string>.HandleAsync(
            Ping command,
            CancellationToken cancellationToken) =>
            Task.FromResult("command");

        Task<string> IQueryHandler<Ping, string>.HandleAsync(
            Ping query,
            CancellationToken cancellationToken) =>
            Task.FromResult("query");
    }

    private sealed class NotAHandler;

    private sealed class RecordingValidator : AbstractValidator<Ping>
    {
        public RecordingValidator(List<string> sequence) =>
            RuleFor(ping => ping.Value).Must(_ =>
            {
                sequence.Add("Validation");
                return true;
            });
    }

    private sealed class RejectingValidator : AbstractValidator<Ping>
    {
        public RejectingValidator() =>
            RuleFor(ping => ping.Value).Must(_ => false).WithMessage("no");
    }

    private sealed class CallbackValidator : AbstractValidator<Ping>
    {
        public CallbackValidator(Action onValidate) =>
            RuleFor(ping => ping.Value).Must(_ =>
            {
                onValidate();
                return true;
            });
    }

    private sealed class CapturingLogger(List<string> sequence) : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull =>
            NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information)
            {
                sequence.Add("Logging:complete");
            }

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), Activity.Current));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Activity? Activity);

    private sealed class CapturingLoggerFactory(CapturingLogger logger) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose()
        {
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private int _reads;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            _reads++;
            return _reads == 1 ? 0 : TimeSpan.FromMilliseconds(42).Ticks;
        }
    }

    public sealed class UnregisteredPingValidator : AbstractValidator<Ping>
    {
        public UnregisteredPingValidator() =>
            RuleFor(ping => ping.Value).Must(_ => false).WithMessage("unregistered");
    }
}
