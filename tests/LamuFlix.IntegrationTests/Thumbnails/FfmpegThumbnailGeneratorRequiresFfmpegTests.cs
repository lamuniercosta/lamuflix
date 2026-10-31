using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests.Thumbnails;

public sealed class FfmpegThumbnailGeneratorRequiresFfmpegTests
{
    private const int MaxWidth = 320;

    [Fact]
    [Trait("Category", "RequiresFfmpeg")]
    public async Task GenerateAsync_ShortVideoFixture_WritesJpegWithinMaxWidth()
    {
        // arrange
        var fixturePath = FixturePath();
        using var temporary = new TemporaryDirectory();
        var options = new ThumbnailOptions
        {
            Enabled = true,
            CacheDirectory = Path.Combine(temporary.Path, "cache"),
            MaxWidth = MaxWidth,
            ProcessTimeoutSeconds = 60,
        };
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(temporary.Path);
        var generator = new FfmpegThumbnailGenerator(
            Options.Create(options),
            new SystemProcessRunner(),
            environment);

        // act
        var result = await generator.GenerateAsync(
            fixturePath,
            TestContext.Current.CancellationToken);

        // assert
        // A missing ffmpeg/ffprobe surfaces as BinaryNotFound here and must fail this required test.
        result.FailureReason.ShouldBeNull(
            "ffmpeg and ffprobe must be installed for the RequiresFfmpeg proof; got failure "
            + result.FailureReason);
        result.Success.ShouldBeTrue();
        result.CachePath.ShouldNotBeNullOrWhiteSpace();
        var cachePath = result.CachePath!;
        cachePath.ShouldStartWith(Path.Combine(temporary.Path, "cache"));
        File.Exists(cachePath).ShouldBeTrue();

        var bytes = await File.ReadAllBytesAsync(cachePath, TestContext.Current.CancellationToken);
        bytes.Length.ShouldBeGreaterThanOrEqualTo(2);
        bytes[0].ShouldBe((byte)0xFF);
        bytes[1].ShouldBe((byte)0xD8);

        var (width, height) = ReadJpegSize(bytes);
        width.ShouldBeGreaterThan(0);
        width.ShouldBeLessThanOrEqualTo(MaxWidth);
        height.ShouldBeGreaterThan(0);
    }

    private static string FixturePath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Thumbnails", "short-video.mp4");
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException("Missing committed short-video fixture.", path);
    }

    private static (int Width, int Height) ReadJpegSize(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            throw new InvalidOperationException("Bytes are not a JPEG stream.");
        }

        var offset = 2;
        while (offset + 3 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = bytes[offset + 1];
            if (IsStandaloneMarker(marker))
            {
                offset += 2;
                continue;
            }

            var segmentLength = (bytes[offset + 2] << 8) | bytes[offset + 3];
            if (IsStartOfFrame(marker) && offset + 8 < bytes.Length)
            {
                var height = (bytes[offset + 5] << 8) | bytes[offset + 6];
                var width = (bytes[offset + 7] << 8) | bytes[offset + 8];
                return (width, height);
            }

            offset += 2 + segmentLength;
        }

        throw new InvalidOperationException("JPEG contains no start-of-frame marker.");
    }

    private static bool IsStandaloneMarker(byte marker) =>
        marker is 0x01 or 0xD8 or 0xD9 or (>= 0xD0 and <= 0xD7);

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-thumbnails-ffmpeg-" + Guid.NewGuid().ToString("N"));
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
