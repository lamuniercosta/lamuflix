using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Streaming;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

// ReSharper disable MethodHasAsyncOverload

namespace LamuFlix.UnitTests.Streaming;

public sealed class StreamConversionServiceTests
{
    private static readonly byte[] ConvertedBytes = [0x00, 0x01, 0x02, 0x03, 0x04];

    public static TheoryData<string> RawCodecs => new()
    {
        "aac",
        "mp3",
        "opus",
        "vorbis",
        "truehd",
    };

    public static TheoryData<string, bool> MalformedProbeOutputs => new()
    {
        { "not-json", false },
        { "{}", false },
        { "{\"streams\":{}}", false },
        { "{\"streams\":[{\"codec_type\":\"audio\"}]}", false },
        { "{\"streams\":[{\"codec_type\":\"audio\",\"codec_name\":\"\"}]}", false },
    };

    [Fact]
    public async Task PrepareAsync_SelectsFirstDefaultAudioTrackAndMapsItsOrdinal()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("aac", false), ("ac3", false), ("dts", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(42, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeTrue();
        var conversion = runner.Snapshot().Single(invocation => invocation.Executable == "ffmpeg");
        conversion.Arguments.ShouldContain("0:a:2");
        conversion.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
        runner.Snapshot().Single(invocation => invocation.Executable == "ffprobe").Timeout
            .ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PrepareAsync_WithoutDefaultAudio_SelectsFirstAudioInInputOrder()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", false), ("eac3", false))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(43, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeTrue();
        var conversion = runner.Snapshot().Single(invocation => invocation.Executable == "ffmpeg");
        conversion.Arguments.ShouldContain("0:a:0");
    }

    [Theory]
    [MemberData(nameof(RawCodecs))]
    public async Task PrepareAsync_LeavesNonConversionCodecsOnRawPath(string codec)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Task.FromResult(Succeeded(Probe((codec, true)))),
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(44, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        result.Path.ShouldBe(source);
        runner.Snapshot().ShouldNotContain(invocation => invocation.Executable == "ffmpeg");
    }

    [Theory]
    [InlineData("ac3")]
    [InlineData("eac3")]
    [InlineData("dts")]
    public async Task PrepareAsync_ConvertsUnsupportedCodecs(string codec)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe((codec, true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(45, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeTrue();
        File.ReadAllBytes(result.Path).ShouldBe(ConvertedBytes);
    }

    [Fact]
    public async Task PrepareAsync_SuccessfulProbeWithoutAudio_StaysRawWithoutWarning()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Task.FromResult(Succeeded(
                "{\"streams\":[{\"codec_type\":\"video\",\"codec_name\":\"hevc\"}]}")),
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var result = await service.PrepareAsync(46, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task PrepareAsync_ProbeFailure_WarnsWithMovieIdAndFallsBackToRaw()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Task.FromResult(new ProcessResult(
                false,
                1,
                string.Empty,
                "probe error",
                "NonZeroExit")),
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var result = await service.PrepareAsync(47, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("47", StringComparison.Ordinal)
            && entry.Message.Contains("probe-failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PrepareAsync_ProbeCancellation_PropagatesWithoutWarning()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, token) => throw new OperationCanceledException(token),
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => service.PrepareAsync(48, source, TestContext.Current.CancellationToken));

        // assert
        exception.ShouldNotBeNull();
        logger.Entries.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(MalformedProbeOutputs))]
    public async Task PrepareAsync_MalformedProbeOutput_WarnsAndFallsBackToRaw(string json, bool unused)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Task.FromResult(Succeeded(json)),
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var result = await service.PrepareAsync(49, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("probe-failed", StringComparison.Ordinal));
        _ = unused;
    }

    [Fact]
    public async Task PrepareAsync_OversizedProbeOutput_WarnsAndFallsBackToRaw()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Task.FromResult(Succeeded(new string('x', 70 * 1024))),
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var result = await service.PrepareAsync(50, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("probe-failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PrepareAsync_ReusesCachedProbeForSameSourceVersion()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        await service.PrepareAsync(51, source, TestContext.Current.CancellationToken);
        await service.PrepareAsync(51, source, TestContext.Current.CancellationToken);

        // assert
        runner.Snapshot().Count(invocation => invocation.Executable == "ffprobe").ShouldBe(1);
    }

    [Fact]
    public async Task PrepareAsync_ReprobesAfterSourceVersionChanges()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        await service.PrepareAsync(52, source, TestContext.Current.CancellationToken);
        TemporaryDirectory.Mutate(source, "new-version");
        await service.PrepareAsync(52, source, TestContext.Current.CancellationToken);

        // assert
        runner.Snapshot().Count(invocation => invocation.Executable == "ffprobe").ShouldBe(2);
    }

    [Fact]
    public async Task PrepareAsync_SourceVersionChangesBeforeConversion_DiscardsStaleWork()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var probeCalls = 0;
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) =>
            {
                if (invocation.Executable != "ffprobe")
                {
                    return Task.FromResult(WriteConversion(invocation));
                }

                probeCalls++;
                if (probeCalls == 1)
                {
                    TemporaryDirectory.Mutate(source, "replaced");
                    return Task.FromResult(Succeeded(Probe(("ac3", true))));
                }

                return Task.FromResult(Succeeded(Probe(("aac", true))));
            },
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(53, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        runner.Snapshot().ShouldNotContain(invocation => invocation.Executable == "ffmpeg");
        Directory.GetFiles(Path.Combine(temporary.Path, "scratch")).ShouldBeEmpty();
    }

    [Fact]
    public async Task PrepareAsync_SourceVersionChangesBeforePublish_DiscardsStaleOutput()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var probeCalls = 0;
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    probeCalls++;
                    return Task.FromResult(Succeeded(
                        Probe((probeCalls == 1 ? "ac3" : "aac", true))));
                }

                TemporaryDirectory.Mutate(source, "replaced");
                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                return Task.FromResult(Succeeded());
            },
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(54, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        probeCalls.ShouldBe(2);
        Directory.GetFiles(Path.Combine(temporary.Path, "scratch")).ShouldBeEmpty();
    }

    [Fact]
    public async Task PrepareAsync_ConcurrentSameVersion_SharesOneConversion()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Invocation? conversion = null;
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    return Task.FromResult(Succeeded(Probe(("ac3", true))));
                }

                conversion = invocation;
                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                started.TrySetResult();
                return release.Task;
            },
        };
        var service = CreateService(temporary, runner);

        // act
        var first = service.PrepareAsync(55, source, TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = service.PrepareAsync(56, source, TestContext.Current.CancellationToken);
        runner.Snapshot().Count(invocation => invocation.Executable == "ffmpeg").ShouldBe(1);
        release.TrySetResult(Succeeded());
        var results = await Task.WhenAll(first, second);

        // assert
        results.ShouldAllBe(result => result.Converted);
        results[0].Path.ShouldBe(results[1].Path);
        conversion.ShouldNotBeNull();
    }

    [Fact]
    public async Task PrepareAsync_CancellingOneWaiter_KeepsConversionForOtherWaiter()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        using var firstWaiter = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ScriptedRunner
        {
            Responder = (invocation, token) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    return Task.FromResult(Succeeded(Probe(("ac3", true))));
                }

                _ = token.Register(() => childCancelled.TrySetResult());
                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                started.TrySetResult();
                return release.Task.WaitAsync(token);
            },
        };
        var service = CreateService(temporary, runner);

        // act
        var first = service.PrepareAsync(57, source, firstWaiter.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = service.PrepareAsync(58, source, TestContext.Current.CancellationToken);
        firstWaiter.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        childCancelled.Task.IsCompleted.ShouldBeFalse();
        release.TrySetResult(Succeeded());
        var result = await second;

        // assert
        result.Converted.ShouldBeTrue();
        runner.Snapshot().Count(invocation => invocation.Executable == "ffmpeg").ShouldBe(1);
    }

    [Fact]
    public async Task PrepareAsync_ReattachWithinGrace_ReusesInFlightChild()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        using var firstWaiter = new CancellationTokenSource();
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ScriptedRunner
        {
            Responder = (invocation, token) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    return Task.FromResult(Succeeded(Probe(("ac3", true))));
                }

                _ = token.Register(() => childCancelled.TrySetResult());
                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                started.TrySetResult();
                return release.Task.WaitAsync(token);
            },
        };
        var service = CreateService(temporary, runner, timeProvider: time);

        // act
        var first = service.PrepareAsync(59, source, firstWaiter.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        firstWaiter.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        await time.WaitForPendingTimerAsync(TestContext.Current.CancellationToken);
        var second = service.PrepareAsync(60, source, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2));
        childCancelled.Task.IsCompleted.ShouldBeFalse();
        release.TrySetResult(Succeeded());
        var result = await second;

        // assert
        result.Converted.ShouldBeTrue();
        runner.Snapshot().Count(invocation => invocation.Executable == "ffmpeg").ShouldBe(1);
    }

    [Fact]
    public async Task PrepareAsync_GraceExpiryAtZeroWaiters_KillsChildAndDeletesPartial()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        using var firstWaiter = new CancellationTokenSource();
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ScriptedRunner
        {
            Responder = (invocation, token) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    return Task.FromResult(Succeeded(Probe(("ac3", true))));
                }

                _ = token.Register(() => childCancelled.TrySetResult());
                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                started.TrySetResult();
                return WaitForCancellationAsync(token);
            },
        };
        var service = CreateService(temporary, runner, timeProvider: time);

        // act
        var first = service.PrepareAsync(61, source, firstWaiter.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var partial = runner.Snapshot().Single(invocation => invocation.Executable == "ffmpeg").Arguments[^1];
        firstWaiter.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        await time.WaitForPendingTimerAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await childCancelled.Task.WaitAsync(TestContext.Current.CancellationToken);

        // assert
        File.Exists(partial).ShouldBeFalse();
        Directory.GetFiles(Path.Combine(temporary.Path, "scratch")).ShouldBeEmpty();
        runner.Snapshot().Count(invocation => invocation.Executable == "ffmpeg").ShouldBe(1);
    }

    [Fact]
    public async Task PrepareAsync_ConversionFailure_CleansUpPartialImmediately()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) =>
            {
                if (invocation.Executable == "ffprobe")
                {
                    return Task.FromResult(Succeeded(Probe(("dts", true))));
                }

                File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
                return Task.FromResult(new ProcessResult(false, 1, string.Empty, "ffmpeg error", "NonZeroExit"));
            },
        };
        var logger = new RecordingLogger();
        var service = CreateService(temporary, runner, logger);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.PrepareAsync(62, source, TestContext.Current.CancellationToken));

        // assert
        exception.Message.ShouldContain("62");
        Directory.GetFiles(Path.Combine(temporary.Path, "scratch")).ShouldBeEmpty();
    }

    [Fact]
    public async Task PrepareAsync_PublishesByAtomicRenameInsideAbsoluteScratch()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);

        // act
        var result = await service.PrepareAsync(63, source, TestContext.Current.CancellationToken);

        // assert
        Path.GetDirectoryName(result.Path).ShouldBe(Path.Combine(temporary.Path, "scratch"));
        result.Path.ShouldEndWith(".mp4");
        File.ReadAllBytes(result.Path).ShouldBe(ConvertedBytes);
        Directory.GetFiles(Path.Combine(temporary.Path, "scratch")).Length.ShouldBe(1);
    }

    [Fact]
    public async Task PrepareAsync_RelativeScratchResolvesUnderContentRoot()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions { ScratchDirectory = "cache/stream-conversion" };
        var service = CreateService(temporary, runner, options: options);

        // act
        var result = await service.PrepareAsync(70, source, TestContext.Current.CancellationToken);

        // assert
        Path.GetDirectoryName(result.Path)
            .ShouldBe(Path.Combine(temporary.Path, "cache", "stream-conversion"));
    }

    [Fact]
    public async Task OpenReadAsync_RangeAbort_DoesNotDeleteCompletedAsset()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);
        var prepared = await service.PrepareAsync(64, source, TestContext.Current.CancellationToken);

        // act
        var lease = await service.OpenReadAsync(prepared, TestContext.Current.CancellationToken);
        lease.Dispose();
        await service.CleanupAsync(TestContext.Current.CancellationToken);

        // assert
        File.Exists(prepared.Path).ShouldBeTrue();
    }

    [Fact]
    public async Task CleanupAsync_IdleExpiry_RemovesUnusedCompletedAsset()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
            IdleExpiry = TimeSpan.FromMinutes(5),
        };
        var service = CreateService(temporary, runner, timeProvider: time, options: options);
        var prepared = await service.PrepareAsync(65, source, TestContext.Current.CancellationToken);

        // act
        time.Advance(TimeSpan.FromMinutes(6));
        await service.CleanupAsync(TestContext.Current.CancellationToken);

        // assert
        File.Exists(prepared.Path).ShouldBeFalse();
    }

    [Fact]
    public async Task CleanupAsync_SizePressure_EvictsLeastRecentlyUsedAssetWithoutActiveReader()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var firstSource = temporary.WriteSource("first.mkv");
        var secondSource = temporary.WriteSource("second.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
            ScratchSizeLimitBytes = 1,
        };
        var service = CreateService(temporary, runner, options: options);
        var first = await service.PrepareAsync(66, firstSource, TestContext.Current.CancellationToken);
        var lease = await service.OpenReadAsync(first, TestContext.Current.CancellationToken);
        await service.PrepareAsync(66, secondSource, TestContext.Current.CancellationToken);

        // act
        await service.CleanupAsync(TestContext.Current.CancellationToken);

        // assert
        File.Exists(first.Path).ShouldBeTrue();
        lease.Dispose();
        await service.CleanupAsync(TestContext.Current.CancellationToken);
        File.Exists(first.Path).ShouldBeFalse();
    }

    [Fact]
    public async Task PrepareAsync_ProductionPath_EvictsIdleAssetsUnderSizePressureButKeepsHeldReaders()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var firstSource = temporary.WriteSource("first.mkv");
        var secondSource = temporary.WriteSource("second.mkv");
        var thirdSource = temporary.WriteSource("third.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
            ScratchSizeLimitBytes = 4,
        };
        var service = CreateService(temporary, runner, options: options);
        var first = await service.PrepareAsync(66, firstSource, TestContext.Current.CancellationToken);

        // act
        var second = await service.PrepareAsync(66, secondSource, TestContext.Current.CancellationToken);

        // assert
        File.Exists(first.Path).ShouldBeFalse();
        File.Exists(second.Path).ShouldBeTrue();
        var lease = await service.OpenReadAsync(second, TestContext.Current.CancellationToken);
        var third = await service.PrepareAsync(66, thirdSource, TestContext.Current.CancellationToken);
        File.Exists(second.Path).ShouldBeTrue();
        File.Exists(third.Path).ShouldBeTrue();
        lease.Dispose();
    }

    [Fact]
    public async Task PrepareAsync_SecondConvertedRequest_ReusesCompletedAssetWithoutNewConversion()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var service = CreateService(temporary, runner);
        var first = await service.PrepareAsync(67, source, TestContext.Current.CancellationToken);
        var lease = await service.OpenReadAsync(first, TestContext.Current.CancellationToken);
        lease.Dispose();

        // act
        var second = await service.PrepareAsync(67, source, TestContext.Current.CancellationToken);

        // assert
        second.Converted.ShouldBeTrue();
        second.Path.ShouldBe(first.Path);
        runner.Snapshot().Count(invocation => invocation.Executable == "ffmpeg").ShouldBe(1);
        var reopened = await service.OpenReadAsync(second, TestContext.Current.CancellationToken);
        reopened.Dispose();
        File.Exists(second.Path).ShouldBeTrue();
    }

    [Fact]
    public async Task PrepareAsync_ProductionPath_EnforcesIdleExpiryWithoutExplicitCleanup()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var firstSource = temporary.WriteSource("first.mkv");
        var secondSource = temporary.WriteSource("second.mkv");
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
            IdleExpiry = TimeSpan.FromMinutes(5),
        };
        var service = CreateService(temporary, runner, timeProvider: time, options: options);
        var first = await service.PrepareAsync(68, firstSource, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(6));

        // act
        var second = await service.PrepareAsync(68, secondSource, TestContext.Current.CancellationToken);

        // assert
        File.Exists(first.Path).ShouldBeFalse();
        File.Exists(second.Path).ShouldBeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_EvictedAsset_ThrowsNotFoundInsteadOfServingUntrackedStream()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
            IdleExpiry = TimeSpan.FromMinutes(5),
        };
        var service = CreateService(temporary, runner, timeProvider: time, options: options);
        var prepared = await service.PrepareAsync(69, source, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(6));
        await service.CleanupAsync(TestContext.Current.CancellationToken);
        File.WriteAllBytes(prepared.Path, [0x09]);

        // act
        var exception = await Record.ExceptionAsync(() =>
            service.OpenReadAsync(prepared, TestContext.Current.CancellationToken));

        // assert
        exception.ShouldBeOfType<FileNotFoundException>();
    }

    [Fact]
    public async Task PrepareAsync_SourceVersionChange_RemovesStaleCompletedAsset()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var probeCalls = 0;
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) =>
            {
                if (invocation.Executable != "ffprobe")
                {
                    return Task.FromResult(WriteConversion(invocation));
                }

                probeCalls++;
                return Task.FromResult(Succeeded(Probe(((probeCalls == 1 ? "ac3" : "aac"), true))));
            },
        };
        var service = CreateService(temporary, runner);
        var prepared = await service.PrepareAsync(67, source, TestContext.Current.CancellationToken);

        // act
        TemporaryDirectory.Mutate(source, "replaced");
        var result = await service.PrepareAsync(67, source, TestContext.Current.CancellationToken);

        // assert
        result.Converted.ShouldBeFalse();
        File.Exists(prepared.Path).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_RemovesOwnedOrphans_AndKeepsForeignFiles()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var scratch = Path.Combine(temporary.Path, "scratch");
        Directory.CreateDirectory(scratch);
        var owned = Path.Combine(scratch, "lfstream-orphan.partial.mp4");
        var foreign = Path.Combine(scratch, "foreign.partial.mp4");
        var completed = Path.Combine(scratch, "lfstream-completed.mp4");
        File.WriteAllText(owned, "orphan");
        File.WriteAllText(foreign, "foreign");
        File.WriteAllText(completed, "completed");
        var runner = new ScriptedRunner();

        // act
        _ = CreateService(temporary, runner);

        // assert
        File.Exists(owned).ShouldBeFalse();
        File.Exists(foreign).ShouldBeTrue();
        File.Exists(completed).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_RemovesOwnedFinalOrphans_AndKeepsNonConformingOwnedNames()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var scratch = Path.Combine(temporary.Path, "scratch");
        Directory.CreateDirectory(scratch);
        var orphan = Path.Combine(scratch, "lfstream-0123456789ABCDEF.mp4");
        var nonConforming = Path.Combine(scratch, "lfstream-completed.mp4");
        var foreign = Path.Combine(scratch, "foreign.mp4");
        File.WriteAllText(orphan, "orphan");
        File.WriteAllText(nonConforming, "completed");
        File.WriteAllText(foreign, "foreign");
        var runner = new ScriptedRunner();

        // act
        _ = CreateService(temporary, runner);

        // assert
        File.Exists(orphan).ShouldBeFalse();
        File.Exists(nonConforming).ShouldBeTrue();
        File.Exists(foreign).ShouldBeTrue();
    }

    [Fact]
    public async Task Constructor_Restart_RemovesOrphanedCompletedFinalAndKeepsForeignFiles()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var source = temporary.WriteSource("movie.mkv");
        var scratch = Path.Combine(temporary.Path, "scratch");
        var runner = new ScriptedRunner
        {
            Responder = (invocation, _) => invocation.Executable == "ffprobe"
                ? Task.FromResult(Succeeded(Probe(("ac3", true))))
                : Task.FromResult(WriteConversion(invocation)),
        };
        var options = new StreamConversionOptions { ScratchDirectory = scratch };
        var first = CreateService(temporary, runner, options: options);
        var prepared = await first.PrepareAsync(70, source, TestContext.Current.CancellationToken);
        File.Exists(prepared.Path).ShouldBeTrue();
        var foreign = Path.Combine(scratch, "foreign.mp4");
        File.WriteAllText(foreign, "foreign");

        // act
        _ = CreateService(temporary, runner, options: options);

        // assert
        File.Exists(prepared.Path).ShouldBeFalse();
        File.Exists(foreign).ShouldBeTrue();
    }

    private static StreamConversionService CreateService(
        TemporaryDirectory temporary,
        ScriptedRunner runner,
        RecordingLogger? logger = null,
        TimeProvider? timeProvider = null,
        StreamConversionOptions? options = null)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(temporary.Path);
        options ??= new StreamConversionOptions
        {
            ScratchDirectory = Path.Combine(temporary.Path, "scratch"),
        };
        return new StreamConversionService(
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Options.Options.Create(new ThumbnailOptions { FfmpegPath = "ffmpeg", ProcessTimeoutSeconds = 30 }),
            runner,
            environment,
            logger ?? new RecordingLogger(),
            timeProvider);
    }

    private static string Probe(params (string Codec, bool Default)[] tracks)
    {
        var entries = tracks.Select(track =>
            "{\"codec_type\":\"audio\",\"codec_name\":\"" + track.Codec
            + "\",\"disposition\":{\"default\":" + (track.Default ? 1 : 0) + "}}");
        return "{\"streams\":[" + string.Join(",", entries) + "]}";
    }

    private static ProcessResult Succeeded(string standardOutput = "") =>
        new(true, 0, standardOutput, string.Empty, null);

    private static async Task<ProcessResult> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return Succeeded();
    }

    private static ProcessResult WriteConversion(Invocation invocation)
    {
        File.WriteAllBytes(invocation.Arguments[^1], ConvertedBytes);
        return Succeeded();
    }

    private sealed record Invocation(
        string Executable,
        IReadOnlyList<string> Arguments,
        TimeSpan Timeout);

    private sealed class ScriptedRunner : IProcessRunner
    {
        private readonly Lock gate = new();

        private readonly List<Invocation> invocations = [];


        public Func<Invocation, CancellationToken, Task<ProcessResult>> Responder { get; set; } =
            (_, _) => Task.FromResult(new ProcessResult(false, null, string.Empty, string.Empty, "NonZeroExit"));

        public Task<ProcessResult> RunAsync(
            string executable,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var invocation = new Invocation(executable, [.. arguments], timeout);
            lock (gate)
            {
                invocations.Add(invocation);
            }

            return Responder(invocation, cancellationToken);
        }

        public IReadOnlyList<Invocation> Snapshot()
        {
            lock (gate)
            {
                return [.. invocations];
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class RecordingLogger : ILogger<StreamConversionService>
    {
        public List<LogEntry> Entries { get; } = [];

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
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-streaming-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string WriteSource(string name, string content = "source")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            return path;
        }

        public static void Mutate(string path, string content)
        {
            File.WriteAllText(path, content);
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
