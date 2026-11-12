using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class SubtitleSidecarStoreConcurrencyTests
{
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string SidecarFileName = "Heat.1995.1080p.srt";

    [Fact]
    public async Task CreateAsync_ConcurrentCreates_ExactlyOneWinsAndTheRestReportAlreadyPresent()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var root = library.Root;
        var ct = TestContext.Current.CancellationToken;
        using var start = new Barrier(2);

        Task<SubtitleSidecarWriteResult> Create(int fill) => Task.Run(
            async () =>
            {
                var store = CreateStore(root);
                start.SignalAndWait(ct);
                return await store.CreateAsync(
                    new LibraryPath(MovieFolder),
                    PrimaryFileName,
                    Enumerable.Repeat((byte)fill, 64).ToArray(),
                    ct);
            },
            ct);

        // act
        var results = await Task.WhenAll(Create(7), Create(9));

        // assert
        results.Count(result => result == SubtitleSidecarWriteResult.Created).ShouldBe(1);
        results.Count(result => result == SubtitleSidecarWriteResult.AlreadyPresent).ShouldBe(1);
        var content = await File.ReadAllBytesAsync(Path.Combine(root, MovieFolder, SidecarFileName), ct);
        content.Length.ShouldBe(64);
        content.ShouldAllBe(value => value == content[0]);
    }

    private static SubtitleSidecarStore CreateStore(string rootPath)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(rootPath);
        var reader = new LibraryFileReader(
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
        return new SubtitleSidecarStore(
            reader,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
    }

    private static byte[] PrimaryPayload() => [0x1A, 0x45, 0xDF, 0xA3, 0x42];

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "lamuflix-subtitle-sidecar-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteFile(string relativePath, byte[] contents)
        {
            var fullPath = Path.Combine(Root, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, contents);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
