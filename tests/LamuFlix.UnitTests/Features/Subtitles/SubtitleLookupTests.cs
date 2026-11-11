using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleLookupTests
{
    private const string Hash = "a1b2c3d4e5f60718";
    private const long FileSize = 4_000_000_000;
    private const string PrimaryTitle = "Blade Runner";
    private static readonly string[] EnglishThenPortuguese = ["en", "pt-BR"];

    [Fact]
    public async Task FindAsync_HashSearchMatchesAPreferredLanguage_ReturnsItWithoutATitleRequest()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [Hash] = [new(1, "en", DownloadCount: 3, MatchesMovieHash: true)],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(1);
        provider.Searches.Count.ShouldBe(1);
        provider.Searches[0].MovieHash.ShouldBe(Hash);
        provider.Searches[0].FileSize.ShouldBe(FileSize);
        provider.Searches[0].Title.ShouldBeNull();
        provider.Searches[0].PreferredLanguages.ShouldBe(EnglishThenPortuguese);
    }

    [Fact]
    public async Task FindAsync_HashSearchOffersNoPreferredLanguage_FallsBackToTheTitleSearch()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [Hash] = [new(1, "de", DownloadCount: 90, MatchesMovieHash: true)],
            [PrimaryTitle] = [new(2, "pt-BR", DownloadCount: 4, MatchesMovieHash: false)],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(2);
        provider.Searches.Count.ShouldBe(2);
        provider.Searches[1].Title.ShouldBe(PrimaryTitle);
        provider.Searches[1].MovieHash.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_IneligibleHash_SkipsTheHashSearchAndUsesTheTitle()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [PrimaryTitle] = [new(5, "en", DownloadCount: 1, MatchesMovieHash: false)],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(MovieHash: null, FileSize: 900, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(5);
        provider.Searches.Count.ShouldBe(1);
        provider.Searches[0].Title.ShouldBe(PrimaryTitle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_BlankTitle_SkipsTheTitleRequestAndReturnsNoMatch(string? title)
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [Hash] = [new(1, "de", DownloadCount: 90, MatchesMovieHash: true)],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, title, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldBeNull();
        provider.Searches.Count.ShouldBe(1);
        provider.Searches.TrueForAll(search => search.Title is null).ShouldBeTrue();
    }

    [Fact]
    public async Task FindAsync_HashMatchAndBlankTitle_StillReturnsTheHashMatch()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [Hash] = [new(1, "en", DownloadCount: 2, MatchesMovieHash: true)],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, Title: "  ", EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_NoCandidatesAnywhere_ReturnsNoMatch()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal));

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldBeNull();
        provider.Searches.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FindAsync_PreferredLanguageTieWithinTheHashSearch_UsesTheDeterministicTieBreak()
    {
        // arrange
        var provider = new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)
        {
            [Hash] =
            [
                new(8, "en", DownloadCount: 10, MatchesMovieHash: false),
                new(9, "en", DownloadCount: 10, MatchesMovieHash: true),
            ],
        });

        // act
        var selected = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(Hash, FileSize, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(9);
    }

    [Fact]
    public async Task FindAsync_NullProvider_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleLookup.FindAsync(
            null!,
            new SubtitleLookupRequest(Hash, FileSize, PrimaryTitle, EnglishThenPortuguese),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task FindAsync_NullRequestTerminal_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleLookup.FindAsync(
            new RecordingProvider(new Dictionary<string, IReadOnlyList<SubtitleCandidate>>(StringComparer.Ordinal)),
            null!,
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    private sealed class RecordingProvider(Dictionary<string, IReadOnlyList<SubtitleCandidate>> results) : ISubtitleProvider
    {
        public List<SubtitleSearch> Searches { get; } = [];

        public Task<IReadOnlyList<SubtitleCandidate>> SearchAsync(SubtitleSearch search, CancellationToken cancellationToken)
        {
            Searches.Add(search);
            var key = search.MovieHash ?? search.Title ?? string.Empty;
            return Task.FromResult(
                results.TryGetValue(key, out var candidates) ? candidates : []);
        }

        public Task<byte[]> DownloadAsync(SubtitleCandidate candidate, CancellationToken cancellationToken) =>
            Task.FromResult(Array.Empty<byte>());
    }
}
