using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LamuFlix.UnitTests.Features;

public sealed class RecordingLogger<T> : ILogger<T>
{
    public IReadOnlyList<LogEntry> Entries => entries;

    private readonly List<LogEntry> entries = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var pairs = new List<KeyValuePair<string, object?>>();
        if (state is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is KeyValuePair<string, object> pair)
                {
                    pairs.Add(new KeyValuePair<string, object?>(pair.Key, pair.Value));
                }
            }
        }

        entries.Add(new LogEntry(logLevel, pairs, exception));
    }

    public sealed record LogEntry(
        LogLevel Level,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
