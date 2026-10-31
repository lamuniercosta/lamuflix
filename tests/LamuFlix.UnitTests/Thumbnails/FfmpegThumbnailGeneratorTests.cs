using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace LamuFlix.UnitTests.Thumbnails;

public sealed class FfmpegThumbnailGeneratorTests
{
    private static readonly byte[] ValidJpeg = [0xFF, 0xD8, 0x01];

    public static TheoryData<string> InvalidDurationProbeOutputs =>
        new()
        {
            "{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{\"duration\":\"0\"}}",
            "{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{\"duration\":\"-1\"}}",
            "{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{\"duration\":\"not-a-number\"}}",
            "{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{\"duration\":\"NaN\"}}",
            "{\"streams\":[{\"codec_type\":\"video\"}]}",
        };

    public static TheoryData<byte[], bool> OutputArtifacts =>
        new()
        {
            { [], false },
            { [0xFF], false },
            { [0xD8, 0xFF], false },
            { [0xFF, 0xD8], true },
            { [0xFF, 0xD8, 0x01, 0x02], true },
        };

    public static TheoryData<string, string> ProbeFailures =>
        new()
        {
            { "LaunchFailed", "BinaryNotFound" },
            { "TimedOut", "ProcessTimedOut" },
            { "NonZeroExit", "ProbeFailed" },
            { "OutputTruncated", "ProbeFailed" },
        };

    public static TheoryData<string, string> ExtractionFailures =>
        new()
        {
            { "LaunchFailed", "BinaryNotFound" },
            { "TimedOut", "ProcessTimedOut" },
            { "NonZeroExit", "ExtractionFailed" },
            { "OutputTruncated", "ExtractionFailed" },
        };

    [Fact]
    public async Task GenerateAsync_UsesTenthOfFractionalDuration_UnderCommaDecimalCulture()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner
        {
            Responder = (executable, arguments) =>
            {
                if (executable == "ffprobe")
                {
                    return Succeeded(ProbeOutput("125.5"));
                }

                File.WriteAllBytes(arguments[^1], ValidJpeg);
                return Succeeded();
            },
        };
        var generator = CreateGenerator(temporary, runner, maxWidth: 320);

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // act
            var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

            // assert
            result.Success.ShouldBeTrue();
            var ffmpegArguments = runner.Invocations[1].Arguments;
            ffmpegArguments[2].ShouldBe("-ss");
            ffmpegArguments[3].ShouldBe("12.55");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task GenerateAsync_BuildsDiscreteArgumentsWithDefaultMaxWidth()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var sourcePath = Path.Combine(temporary.Path, "folder with spaces", "movie file.mkv");
        var runner = new ScriptedRunner
        {
            Responder = (executable, arguments) =>
            {
                if (executable == "ffprobe")
                {
                    return Succeeded(ProbeOutput("100"));
                }

                File.WriteAllBytes(arguments[^1], ValidJpeg);
                return Succeeded();
            },
        };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(sourcePath, TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeTrue();
        var arguments = runner.Invocations[1].Arguments;
        runner.Invocations[1].Executable.ShouldBe("ffmpeg");
        runner.Invocations[1].Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        arguments.Count.ShouldBe(13);
        arguments[0].ShouldBe("-nostdin");
        arguments[1].ShouldBe("-y");
        arguments[2].ShouldBe("-ss");
        arguments[3].ShouldBe("10");
        arguments[4].ShouldBe("-i");
        arguments[5].ShouldBe(sourcePath);
        arguments[6].ShouldBe("-map");
        arguments[7].ShouldBe("0:v:0");
        arguments[8].ShouldBe("-frames:v");
        arguments[9].ShouldBe("1");
        arguments[10].ShouldBe("-vf");
        arguments[11].ShouldBe("scale=min(320\\,iw):-2");
        arguments[12].ShouldStartWith(Path.Combine(temporary.Path, "cache"));
        arguments[12].ShouldEndWith(".jpg");
    }

    [Fact]
    public async Task GenerateAsync_SubstitutesConfiguredMaxWidthIntoScale()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner
        {
            Responder = (executable, arguments) =>
            {
                if (executable == "ffprobe")
                {
                    return Succeeded(ProbeOutput("100"));
                }

                File.WriteAllBytes(arguments[^1], ValidJpeg);
                return Succeeded();
            },
        };
        var generator = CreateGenerator(temporary, runner, maxWidth: 640);

        // act
        await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        runner.Invocations[1].Arguments[11].ShouldBe("scale=min(640\\,iw):-2");
    }

    [Theory]
    [MemberData(nameof(OutputArtifacts))]
    public async Task GenerateAsync_ValidatesJpegStartOfImage(byte[] artifact, bool expectedSuccess)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner
        {
            Responder = (executable, arguments) =>
            {
                if (executable == "ffprobe")
                {
                    return Succeeded(ProbeOutput("100"));
                }

                File.WriteAllBytes(arguments[^1], artifact);
                return Succeeded();
            },
        };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBe(expectedSuccess);
        var outputPath = runner.Invocations[1].Arguments[^1];
        if (expectedSuccess)
        {
            result.CachePath.ShouldBe(outputPath);
            File.Exists(outputPath).ShouldBeTrue();
        }
        else
        {
            result.FailureReason.ShouldBe("InvalidOutput");
            result.CachePath.ShouldBeNull();
            File.Exists(outputPath).ShouldBeFalse();
        }
    }

    [Theory]
    [MemberData(nameof(ProbeFailures))]
    public async Task GenerateAsync_MapsProbeFailures(string processFailure, string expectedFailure)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => new ProcessResult(false, null, string.Empty, string.Empty, processFailure),
        };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "missing.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.CachePath.ShouldBeNull();
        result.FailureReason.ShouldBe(expectedFailure);
        runner.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_ProbeWithoutVideoStream_ReturnsNoVideoStream()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner
        {
            Responder = (_, _) => Succeeded("{\"streams\":[],\"format\":{\"duration\":\"10\"}}"),
        };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("NoVideoStream");
        runner.Invocations.Count.ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(InvalidDurationProbeOutputs))]
    public async Task GenerateAsync_InvalidDuration_ReturnsInvalidDuration(string probeOutput)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner { Responder = (_, _) => Succeeded(probeOutput) };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("InvalidDuration");
        runner.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_MalformedProbeOutput_ReturnsProbeFailed()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner { Responder = (_, _) => Succeeded("not-json") };
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("ProbeFailed");
    }

    [Theory]
    [MemberData(nameof(ExtractionFailures))]
    public async Task GenerateAsync_MapsExtractionFailures(string processFailure, string expectedFailure)
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner { Responder = (_, _) => Succeeded(ProbeOutput("100")) };
        var generator = CreateGenerator(temporary, runner);
        runner.Responder = (executable, _) => executable == "ffprobe"
            ? Succeeded(ProbeOutput("100"))
            : new ProcessResult(false, null, string.Empty, string.Empty, processFailure);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe(expectedFailure);
        runner.Invocations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateAsync_Disabled_DoesNoProcessOrDiskWork()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cacheDirectory = Path.Combine(temporary.Path, "disabled-cache");
        var options = new ThumbnailOptions
        {
            Enabled = false,
            CacheDirectory = cacheDirectory,
        };
        var runner = new ScriptedRunner();
        var generator = CreateGenerator(options, runner, temporary);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("Disabled");
        runner.Invocations.ShouldBeEmpty();
        Directory.Exists(cacheDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_BlankSourcePath_ReturnsInvalidSourcePathWithoutProcess()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner();
        var generator = CreateGenerator(temporary, runner);

        // act
        var result = await generator.GenerateAsync("   ", TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("InvalidSourcePath");
        runner.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_CacheDirectoryIsAFile_ReturnsStorageError()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cachePath = Path.Combine(temporary.Path, "cache-is-a-file");
        await File.WriteAllTextAsync(cachePath, "not a directory", TestContext.Current.CancellationToken);
        var options = new ThumbnailOptions
        {
            Enabled = true,
            CacheDirectory = cachePath,
        };
        var runner = new ScriptedRunner { Responder = (_, _) => Succeeded(ProbeOutput("100")) };
        var generator = CreateGenerator(options, runner, temporary);

        // act
        var result = await generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken);

        // assert
        result.FailureReason.ShouldBe("StorageError");
        runner.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_CallerCancellation_CleansUpPartialOutputAndPropagates()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var runner = new ScriptedRunner { Responder = (_, _) => Succeeded(ProbeOutput("100")) };
        runner.Responder = (executable, arguments) =>
        {
            if (executable == "ffprobe")
            {
                return Succeeded(ProbeOutput("100"));
            }

            File.WriteAllBytes(arguments[^1], ValidJpeg);
            throw new OperationCanceledException();
        };
        var generator = CreateGenerator(temporary, runner);

        // act
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => generator.GenerateAsync(Path.Combine(temporary.Path, "movie.mkv"), TestContext.Current.CancellationToken));

        // assert
        exception.ShouldNotBeNull();
        var outputPath = runner.Invocations[1].Arguments[^1];
        File.Exists(outputPath).ShouldBeFalse();
    }

    private static FfmpegThumbnailGenerator CreateGenerator(
        TemporaryDirectory temporary,
        IProcessRunner runner,
        int maxWidth = 320) =>
        CreateGenerator(
            new ThumbnailOptions
            {
                Enabled = true,
                CacheDirectory = Path.Combine(temporary.Path, "cache"),
                MaxWidth = maxWidth,
            },
            runner,
            temporary);

    private static FfmpegThumbnailGenerator CreateGenerator(
        ThumbnailOptions options,
        IProcessRunner runner,
        TemporaryDirectory temporary)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(temporary.Path);
        return new FfmpegThumbnailGenerator(Microsoft.Extensions.Options.Options.Create(options), runner, environment);
    }

    private static ProcessResult Succeeded(string standardOutput = "") =>
        new(true, 0, standardOutput, string.Empty, null);

    private static string ProbeOutput(string duration) =>
        "{\"streams\":[{\"codec_type\":\"video\",\"duration\":\""
        + duration
        + "\"}],\"format\":{\"duration\":\""
        + duration
        + "\"}}";

    private sealed record Invocation(
        string Executable,
        IReadOnlyList<string> Arguments,
        TimeSpan Timeout);

    private sealed class ScriptedRunner : IProcessRunner
    {
        public List<Invocation> Invocations { get; } = [];

        public Func<string, IReadOnlyList<string>, ProcessResult> Responder { get; set; } =
            (_, _) => new ProcessResult(false, null, string.Empty, string.Empty, "NonZeroExit");

        public Task<ProcessResult> RunAsync(
            string executable,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add(new Invocation(executable, arguments, timeout));
            return Task.FromResult(Responder(executable, arguments));
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-thumbnails-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

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
