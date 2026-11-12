using System;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleCandidateBinderTests
{
    private const int TokenHexLength = 32;
    private static readonly DateTimeOffset Now = new(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);
    private static readonly MovieId Movie = new(42);
    private static readonly MovieId OtherMovie = new(43);
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const int SubtitlesCapacity = 1000;

    [Fact]
    public void Issue_ReturnsOpaqueThirtyTwoHexCharacterToken()
    {
        // arrange
        var binder = CreateBinder();

        // act
        var candidateId = Issue(binder, Movie);

        // assert
        candidateId.Length.ShouldBe(TokenHexLength);
        candidateId.All(static character => char.IsAsciiHexDigitLower(character)).ShouldBeTrue();
    }

    [Fact]
    public void Issue_Repeated_CreatesDistinctTokens()
    {
        // arrange
        var binder = CreateBinder();

        // act
        var ids = Enumerable.Range(0, 20).Select(_ => Issue(binder, Movie)).ToArray();

        // assert
        ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Length);
    }

    [Fact]
    public void TryResolve_FreshToken_ReturnsTheBoundCandidate()
    {
        // arrange
        var binder = CreateBinder();
        var candidateId = Issue(binder, Movie);

        // act
        var resolved = binder.TryResolve(candidateId, Movie, out var binding);

        // assert
        resolved.ShouldBeTrue();
        var bound = binding.ShouldNotBeNull();
        bound.CandidateId.ShouldBe(candidateId);
        bound.Language.ShouldBe("pt-BR");
        bound.ReleaseName.ShouldBe("Dune.2021.1080p");
        bound.Candidate.FileId.ShouldBe(101);
    }

    [Fact]
    public void TryResolve_UnknownToken_ReturnsFalse()
    {
        // arrange
        var binder = CreateBinder();

        // act
        var resolved = binder.TryResolve("0123456789abcdef0123456789abcdef", Movie, out _);

        // assert
        resolved.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_BlankToken_ReturnsFalse(string? candidateId)
    {
        // arrange
        var binder = CreateBinder();

        // act
        var resolved = binder.TryResolve(candidateId, Movie, out _);

        // assert
        resolved.ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_ExpiredToken_ReturnsFalseWithoutPaidSideEffects()
    {
        // arrange
        var clock = new MutableTimeProvider(Now);
        var binder = CreateBinder(clock);
        var candidateId = Issue(binder, Movie);

        // act
        clock.Advance(Ttl + TimeSpan.FromSeconds(1));
        var resolved = binder.TryResolve(candidateId, Movie, out _);

        // assert
        resolved.ShouldBeFalse();
        binder.Count.ShouldBe(0);
    }

    [Fact]
    public void TryResolve_TokenIssuedForAnotherMovie_ReturnsFalse()
    {
        // arrange
        var binder = CreateBinder();
        var candidateId = Issue(binder, Movie);

        // act
        var resolved = binder.TryResolve(candidateId, OtherMovie, out _);

        // assert
        resolved.ShouldBeFalse();
    }

    [Fact]
    public void Issue_BeyondCapacity_EvictsEarliestExpiryAndKeepsIssuing()
    {
        // arrange
        var clock = new MutableTimeProvider(Now);
        var binder = new SubtitleCandidateBinder(clock, new Random(7), () => Ttl, () => 2);
        var first = Issue(binder, Movie);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = Issue(binder, Movie);

        // act
        clock.Advance(TimeSpan.FromSeconds(1));
        var third = Issue(binder, Movie);

        // assert
        binder.Count.ShouldBe(2);
        binder.TryResolve(first, Movie, out _).ShouldBeFalse();
        binder.TryResolve(second, Movie, out _).ShouldBeTrue();
        binder.TryResolve(third, Movie, out _).ShouldBeTrue();
    }

    [Fact]
    public void Issue_SameExpiry_EvictsInFirstInFirstOutOrder()
    {
        // arrange
        var clock = new MutableTimeProvider(Now);
        var binder = new SubtitleCandidateBinder(clock, new Random(11), () => Ttl, () => 2);
        var first = Issue(binder, Movie);
        var second = Issue(binder, Movie);

        // act
        var third = Issue(binder, Movie);

        // assert
        binder.Count.ShouldBe(2);
        binder.TryResolve(first, Movie, out _).ShouldBeFalse();
        binder.TryResolve(second, Movie, out _).ShouldBeTrue();
        binder.TryResolve(third, Movie, out _).ShouldBeTrue();
    }

    [Fact]
    public void Count_NewBinder_IsEmpty()
    {
        // act
        var binder = CreateBinder();

        // assert
        binder.Count.ShouldBe(0);
    }

    [Fact]
    public void Issue_NullMovie_Throws()
    {
        // arrange
        var binder = CreateBinder();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => { _ = binder.Issue(null!, "pt-BR", Candidate(), "Dune.2021.1080p"); };

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Issue_BlankLanguage_Throws()
    {
        // arrange
        var binder = CreateBinder();

        // act
        var act = () => { _ = binder.Issue(Movie, " ", Candidate(), "Dune.2021.1080p"); };

        // assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void TryResolve_NullMovie_Throws()
    {
        // arrange
        var binder = CreateBinder();
        var candidateId = Issue(binder, Movie);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => { _ = binder.TryResolve(candidateId, null!, out _); };

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    private static SubtitleCandidateBinder CreateBinder(TimeProvider? clock = null) =>
        new(clock ?? new MutableTimeProvider(Now), new Random(3), () => Ttl, () => SubtitlesCapacity);

    private static string Issue(SubtitleCandidateBinder binder, MovieId movieId) =>
        binder.Issue(movieId, "pt-BR", Candidate(), "Dune.2021.1080p");

    private static SubtitleCandidate Candidate() =>
        new(FileId: 101, Language: "pt-BR", DownloadCount: 12, MatchesMovieHash: true, ReleaseName: "Dune.2021.1080p");

    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset current = start;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan delta) => current = current.Add(delta);
    }
}
