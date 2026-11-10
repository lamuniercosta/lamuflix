using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Import;

public sealed class ImportCandidatesQueryHandlerTests
{
    private readonly ILibraryFolderDiscovery discovery = Substitute.For<ILibraryFolderDiscovery>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();

    [Fact]
    public async Task HandleAsync_EmptyRoot_ReturnsEmpty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        discovery.DiscoverAsync(TestContext.Current.CancellationToken).Returns(AsyncFolders([]));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);

        // act
        var result = await handler.HandleAsync(new ImportCandidatesQuery(), ct);

        // assert
        result.NewFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AllImported_ReturnsEmpty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var imported = Folder("Heat");
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { imported });
        discovery.DiscoverAsync(TestContext.Current.CancellationToken).Returns(AsyncFolders([imported]));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);

        // act
        var result = await handler.HandleAsync(new ImportCandidatesQuery(), ct);

        // assert
        result.NewFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Mixed_ReturnsOnlyNewInOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var imported = Folder("Heat");
        var firstNew = Folder("Alpha");
        var secondNew = Folder("Bravo");
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { imported });
        discovery.DiscoverAsync(TestContext.Current.CancellationToken)
            .Returns(AsyncFolders([firstNew, imported, secondNew]));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);

        // act
        var result = await handler.HandleAsync(new ImportCandidatesQuery(), ct);

        // assert
        result.NewFolders.Select(path => path.Value).ShouldBe([firstNew.Value, secondNew.Value]);
    }

    [Fact]
    public async Task HandleAsync_CaseOnlyDuplicate_ReturnsSkipped()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var imported = Folder("Heat");
        var duplicate = Folder("HEAT");
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { imported });
        discovery.DiscoverAsync(TestContext.Current.CancellationToken).Returns(AsyncFolders([duplicate]));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);

        // act
        var result = await handler.HandleAsync(new ImportCandidatesQuery(), ct);

        // assert
        result.NewFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DiscoveryFault_Propagates()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var fault = new DirectoryNotFoundException("root missing");
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        discovery.DiscoverAsync(TestContext.Current.CancellationToken).Returns(AsyncFoldersFault(fault));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);

        // act
        var thrown = await Should.ThrowAsync<DirectoryNotFoundException>(
            () => handler.HandleAsync(new ImportCandidatesQuery(), ct));

        // assert
        thrown.ShouldBeSameAs(fault);
    }

    [Fact]
    public async Task HandleAsync_CancelledToken_Propagates()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        discovery.DiscoverAsync(ct).Returns(AsyncFolders([Folder("Heat")]));
        var handler = new ImportCandidatesQueryHandler(discovery, movies);
        await cts.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new ImportCandidatesQuery(), ct));

        // assert
        await movies.DidNotReceive().FindFailedMovieIdsAsync(Arg.Any<CancellationToken>());
    }

    private static LibraryPath Folder(string value) => new(value);

    private static async IAsyncEnumerable<LibraryPath> AsyncFolders(IReadOnlyList<LibraryPath> folders)
    {
        foreach (var folder in folders)
        {
            yield return folder;
        }

        await Task.CompletedTask;
    }

    private static IAsyncEnumerable<LibraryPath> AsyncFoldersFault(Exception fault) =>
        new FaultingEnumerable(fault);

    private sealed class FaultingEnumerable(Exception fault) : IAsyncEnumerable<LibraryPath>
    {
        public IAsyncEnumerator<LibraryPath> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new FaultingEnumerator(fault);

        private sealed class FaultingEnumerator(Exception fault) : IAsyncEnumerator<LibraryPath>
        {
            public LibraryPath Current => throw new NotSupportedException();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public ValueTask<bool> MoveNextAsync()
            {
                throw fault;
            }
        }
    }
}
