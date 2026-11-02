using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Features.Import;

public sealed class ImportLibraryCommandHandlerResultTests
{
    [Fact]
    public async Task HandleAsync_EmptyReadableRoot_ReturnsImportedCountZero()
    {
        // arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/library/Empty");
        var discovery = new FileSystemLibraryFolderDiscovery(
            fileSystem,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "/library" }));
        var importer = new ImporterDouble(expectedConcurrency: 1);
        var handler = Handler(discovery, importer);

        // act
        var result = await handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken);

        // assert
        result.ImportedCount.ShouldBe(0);
        importer.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_MissingOrUnreadableRoot_FailsFast()
    {
        // arrange
        var discovery = new FileSystemLibraryFolderDiscovery(
            new MockFileSystem(),
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "/library" }));
        var importer = new ImporterDouble(expectedConcurrency: 1);
        var handler = Handler(discovery, importer);

        // act
        await Should.ThrowAsync<DirectoryNotFoundException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        importer.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_UnreadableSubtree_FailsFastAfterShutdown()
    {
        // arrange
        var fault = new UnauthorizedAccessException("subtree unreadable");
        var folders = Folders(3);
        var importer = new ImporterDouble(expectedConcurrency: 1);
        var discovery = new DiscoveryDouble(folders, yielded => yielded == 1 ? fault : null)
        {
            BeforeFailure = importer.FirstImportCommitted.Task,
        };
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Calls.ShouldBe(1);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_DisappearingCandidate_FailsFastAfterShutdown()
    {
        // arrange
        var fault = new FileNotFoundException("candidate disappeared");
        var folders = Folders(3);
        var discovery = new DiscoveryDouble(folders);
        var importer = new ImporterDouble(expectedConcurrency: 1, failureAtCall: call => call == 1 ? fault : null);
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<FileNotFoundException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Calls.ShouldBe(1);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_InvalidCandidate_FailsFastAfterShutdown()
    {
        // arrange
        var fault = new InvalidOperationException("candidate has no supported video");
        var discovery = new DiscoveryDouble(Folders(3));
        var importer = new ImporterDouble(expectedConcurrency: 1, failureAtCall: call => call == 1 ? fault : null);
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ScannerFault_PropagatesFirstOriginalFaultRetainingEarlierCommits()
    {
        // arrange
        var fault = new InvalidOperationException("scanner fault");
        var folders = Folders(4);
        var importer = new ImporterDouble(expectedConcurrency: 1);
        var discovery = new DiscoveryDouble(folders, yielded => yielded == 1 ? fault : null)
        {
            BeforeFailure = importer.FirstImportCommitted.Task,
        };
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Imported.ShouldContain(folders[0]);
    }

    [Fact]
    public async Task HandleAsync_ImportFault_PropagatesFirstOriginalFaultRetainingEarlierCommits()
    {
        // arrange
        var fault = new InvalidOperationException("import fault");
        var folders = Folders(4);
        var discovery = new DiscoveryDouble(folders);
        var importer = new ImporterDouble(expectedConcurrency: 1, failureAtCall: call => call == 2 ? fault : null);
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Imported.ShouldBe([folders[0]]);
    }

    [Fact]
    public async Task HandleAsync_PersistenceFault_PropagatesFirstOriginalFaultRetainingEarlierCommits()
    {
        // arrange
        var fault = new DbUpdateException("duplicate library path");
        var folders = Folders(4);
        var discovery = new DiscoveryDouble(folders);
        var importer = new ImporterDouble(expectedConcurrency: 1, failureAtCall: call => call == 2 ? fault : null);
        var handler = Handler(discovery, importer);

        // act
        var thrown = await Should.ThrowAsync<DbUpdateException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.Imported.ShouldBe([folders[0]]);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    private static ImportLibraryCommandHandler Handler(ILibraryFolderDiscovery discovery, IMovieFolderImporter importer) =>
        new(
            discovery,
            importer,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "/library", MaxConcurrency = 1 }));

    private static IReadOnlyList<LibraryPath> Folders(int count) =>
        [.. Enumerable.Range(0, count).Select(index => new LibraryPath($"movie-{index:000}"))];
}
