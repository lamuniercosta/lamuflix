using System;
using System.IO;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class MovieHashReaderTests
{
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.mkv";

    [Fact]
    public async Task ReadAsync_EligibleVideo_ReturnsTheReferenceHashOfItsHeadAndTailWindows()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = MovieHashTests.Payload("increment", MovieHash.MinimumEligibleLength);
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), payload);
        var reader = CreateReader(library.Root);

        // act
        var lookup = await MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
            TestContext.Current.CancellationToken);

        // assert
        lookup.FileLength.ShouldBe(payload.Length);
        lookup.MovieHash.ShouldBe("a0601fdf9f610000");
    }

    [Fact]
    public async Task ReadAsync_VideoOneByteUnderTheMinimum_ReturnsNoHashWithItsLength()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = new byte[MovieHash.MinimumEligibleLength - 1];
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), payload);
        var reader = CreateReader(library.Root);

        // act
        var lookup = await MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
            TestContext.Current.CancellationToken);

        // assert
        lookup.MovieHash.ShouldBeNull();
        lookup.FileLength.ShouldBe(payload.Length);
    }

    [Fact]
    public async Task ReadAsync_ExactlyTheMinimumLength_HashesBothWindows()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = new byte[MovieHash.MinimumEligibleLength];
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), payload);
        var reader = CreateReader(library.Root);

        // act
        var lookup = await MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
            TestContext.Current.CancellationToken);

        // assert
        lookup.MovieHash.ShouldBe("0000000000020000");
    }

    [Fact]
    public async Task ReadAsync_OnlyTheHeadAndTailAffectTheHash_InteriorBytesAreIgnored()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = new byte[MovieHash.MinimumEligibleLength * 2];
        var mutated = (byte[])payload.Clone();
        mutated[MovieHash.WindowLength] = 0xFF;
        mutated[MovieHash.WindowLength * 2] = 0xFF;
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), mutated);
        var reader = CreateReader(library.Root);

        // act
        var lookup = await MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
            TestContext.Current.CancellationToken);

        // assert
        lookup.MovieHash.ShouldBe("0000000000040000");
    }

    [Fact]
    public async Task ReadAsync_NonSeekableStream_ReturnsNoHashInsteadOfThrowing()
    {
        // arrange
        var reader = Substitute.For<ILibraryFileReader>();
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(_ => new NonSeekableStream());

        // act
        var lookup = await MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(PrimaryFileName),
            TestContext.Current.CancellationToken);

        // assert
        lookup.MovieHash.ShouldBeNull();
    }

    [Fact]
    public async Task ReadAsync_OutOfRangeHashWindow_FailsRatherThanReadingShort()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), new byte[MovieHash.MinimumEligibleLength]);
        var reader = Substitute.For<ILibraryFileReader>();
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(new ShortReportingStream(MovieHash.MinimumEligibleLength));

        // act
        var act = () => MovieHashReader.ReadAsync(
            reader,
            new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<EndOfStreamException>(act);
    }

    [Fact]
    public async Task ReadAsync_NullReader_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => MovieHashReader.ReadAsync(
            null!,
            new LibraryPath(PrimaryFileName),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    private static LibraryFileReader CreateReader(string rootPath)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(rootPath);
        return new LibraryFileReader(
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
    }

    private sealed class NonSeekableStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ShortReportingStream(long reportedLength) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => reportedLength;

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Position += count;
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = offset;
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-movie-hash-" + Guid.NewGuid().ToString("N"));
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
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
                // A leftover temp directory must never fail an assertion.
            }
            catch (UnauthorizedAccessException)
            {
                // Same: cleanup is best-effort.
            }
        }
    }
}
