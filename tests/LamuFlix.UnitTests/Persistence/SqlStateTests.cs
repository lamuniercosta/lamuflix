using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.UnitTests.Persistence;

public sealed class SqlStateTests
{
    [Theory]
    [InlineData("40001", null)]
    [InlineData("40001", "IX_movies_imdb_id")]
    [InlineData("40P01", null)]
    [InlineData("40P01", "PK_movies")]
    public void IsRetryableSqlState_SerializationFailureOrDeadlock_ReturnsTrue(string? sqlState, string? constraint)
    {
        // act
        var retryable = EfMovieRepository.IsRetryableSqlState(sqlState, constraint);

        // assert
        retryable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("23505", "PK_movies")]
    [InlineData("23505", "PK_movie_genres")]
    public void IsRetryableSqlState_UniqueViolationOnMovieOrJoinKey_ReturnsTrue(string? sqlState, string? constraint)
    {
        // act
        var retryable = EfMovieRepository.IsRetryableSqlState(sqlState, constraint);

        // assert
        retryable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("23505", "IX_movies_imdb_id")]
    [InlineData("23505", null)]
    [InlineData("23505", "")]
    [InlineData("23505", "PK_directors")]
    [InlineData("XX000", null)]
    [InlineData("XX000", "PK_movies")]
    [InlineData(null, null)]
    public void IsRetryableSqlState_OtherStates_ReturnsFalse(string? sqlState, string? constraint)
    {
        // act
        var retryable = EfMovieRepository.IsRetryableSqlState(sqlState, constraint);

        // assert
        retryable.ShouldBeFalse();
    }

    [Theory]
    [InlineData("PK_movies")]
    [InlineData("PK_movie_genres")]
    public void IsMovieOrJoinPrimaryKey_MovieOrJoinKey_ReturnsTrue(string constraint)
    {
        // act
        var match = EfMovieRepository.IsMovieOrJoinPrimaryKey(constraint);

        // assert
        match.ShouldBeTrue();
    }

    [Theory]
    [InlineData("IX_movies_imdb_id")]
    [InlineData("PK_directors")]
    [InlineData("")]
    [InlineData(null)]
    public void IsMovieOrJoinPrimaryKey_OtherOrMissingKey_ReturnsFalse(string? constraint)
    {
        // act
        var match = EfMovieRepository.IsMovieOrJoinPrimaryKey(constraint);

        // assert
        match.ShouldBeFalse();
    }
}
