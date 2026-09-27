using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Pipeline;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace LamuFlix.Test;

public class LoggingDecoratorTests
{
    private const string PayloadSecret = "payload-secret-9f3a";

    [Fact]
    public async Task HandleAsync_Success_LogsInformationWithElapsedAndNoPayload()
    {
        // arrange
        var logger = new CapturingLogger();
        var decorator = CreateDecorator(logger, request => Task.FromResult(request.Name));

        // act
        var result = await decorator.HandleAsync(new SampleRequest(PayloadSecret), CancellationToken.None);

        // assert
        result.ShouldBe(PayloadSecret);
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("42");
        entry.Message.ShouldContain(nameof(SampleRequest));
        entry.Message.ShouldNotContain(PayloadSecret);
    }

    [Fact]
    public async Task HandleAsync_ValidationException_LogsWarningAndRethrowsWithoutPayload()
    {
        // arrange
        var logger = new CapturingLogger();
        var decorator = CreateDecorator(
            logger,
            _ => throw new ValidationException(new Dictionary<string, string[]>()));

        // act
        await Should.ThrowAsync<ValidationException>(
            () => decorator.HandleAsync(new SampleRequest(PayloadSecret), CancellationToken.None));

        // assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldNotContain(PayloadSecret);
    }

    [Fact]
    public async Task HandleAsync_OtherException_LogsErrorAndRethrowsWithoutPayload()
    {
        // arrange
        var logger = new CapturingLogger();
        var decorator = CreateDecorator(logger, _ => throw new InvalidOperationException("boom"));

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => decorator.HandleAsync(new SampleRequest(PayloadSecret), CancellationToken.None));

        // assert
        exception.Message.ShouldBe("boom");
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldNotContain(PayloadSecret);
    }

    private static LoggingDecorator<SampleRequest, string> CreateDecorator(
        CapturingLogger logger,
        Func<SampleRequest, Task<string>> inner) =>
        new(
            (request, _) => inner(request),
            logger,
            new FixedTimeProvider());

    private sealed record SampleRequest(string Name);

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

    private sealed class CapturingLogger : ILogger
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
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);
}
