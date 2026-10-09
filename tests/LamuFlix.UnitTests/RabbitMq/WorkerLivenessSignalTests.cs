using System;
using System.IO.Abstractions.TestingHelpers;
using FsCheck.Xunit;
using LamuFlix.Infrastructure.RabbitMq;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class WorkerLivenessSignalTests
{
    [Fact]
    public void MarkListening_WritesTheClock_AndMarkStopped_RemovesTheFile()
    {
        // arrange
        var instant = new DateTimeOffset(2026, 10, 9, 16, 0, 0, TimeSpan.Zero);
        var fileSystem = new MockFileSystem();
        var signal = new WorkerLivenessSignal(fileSystem, new FixedTimeProvider(instant), @"C:\worker");

        // act
        signal.MarkListening();
        var body = fileSystem.File.ReadAllText(fileSystem.Path.Combine(@"C:\worker", WorkerLivenessSignal.FileName));
        signal.MarkStopped();

        // assert
        body.ShouldBe(instant.ToString("O"));
        fileSystem.File.Exists(fileSystem.Path.Combine(@"C:\worker", WorkerLivenessSignal.FileName)).ShouldBeFalse();
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool MarkListening_AnyInstant_RoundTripsAndMarkStopped_LeavesNoFile(long rawMilliseconds)
    {
        var instant = Instant(rawMilliseconds);
        var fileSystem = new MockFileSystem();
        var signal = new WorkerLivenessSignal(fileSystem, new FixedTimeProvider(instant), @"C:\worker");
        var path = fileSystem.Path.Combine(@"C:\worker", WorkerLivenessSignal.FileName);

        signal.MarkListening();
        var written = fileSystem.File.Exists(path) && fileSystem.File.ReadAllText(path) == instant.ToString("O");
        signal.MarkStopped();

        return written && !fileSystem.File.Exists(path);
    }

    private static DateTimeOffset Instant(long rawMilliseconds)
    {
        const long min = -62135596800000L;
        const long max = 253402300799999L;
        var span = max - min;
        var offset = rawMilliseconds % span;
        if (offset < 0)
        {
            offset += span;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(min + offset);
    }

    private sealed class FixedTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
