using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryValidatorTests
{
    private readonly MovieQueryValidator validator = new();

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void PageSize_Boundaries(int size, bool valid)
    {
        var result = validator.Validate(Accepted() with { Page = new Page(1, size) });

        result.IsValid.ShouldBe(valid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void PageNumber_Boundaries(int number, bool valid)
    {
        var result = validator.Validate(Accepted() with { Page = new Page(number, 20) });

        result.IsValid.ShouldBe(valid);
    }

    [Fact]
    public void Runtime_MinEqualsMax_IsValid()
    {
        validator.Validate(Accepted() with { Runtime = new RuntimeRange(90, 90, false) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Runtime_MinGreaterThanMax_IsInvalid()
    {
        validator.Validate(Accepted() with { Runtime = new RuntimeRange(100, 90, false) }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Runtime_OneBoundNull_IsValid()
    {
        validator.Validate(Accepted() with { Runtime = new RuntimeRange(90, null, true) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Runtime_EmptyRange_IsValid()
    {
        validator.Validate(Accepted() with { Runtime = new RuntimeRange(null, null, false) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Year_MinEqualsMax_IsValid()
    {
        validator.Validate(Accepted() with { Year = new YearRange(1999, 1999) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Year_MinGreaterThanMax_IsInvalid()
    {
        validator.Validate(Accepted() with { Year = new YearRange(2000, 1999) }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Year_OneBoundNull_IsValid()
    {
        validator.Validate(Accepted() with { Year = new YearRange(null, 1999) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Year_EmptyRange_IsValid()
    {
        validator.Validate(Accepted() with { Year = new YearRange(null, null) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Sort_Null_IsInvalid()
    {
        validator.Validate(Accepted() with { Sort = null }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Direction_Null_IsInvalid()
    {
        validator.Validate(Accepted() with { Direction = null }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Rating_MinEqualsMax_IsValid()
    {
        validator.Validate(Accepted() with { Rating = new RatingRange(7m, 7m) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Rating_MinGreaterThanMax_IsInvalid()
    {
        validator.Validate(Accepted() with { Rating = new RatingRange(8m, 7m) }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Rating_OneBoundNull_IsValid()
    {
        validator.Validate(Accepted() with { Rating = new RatingRange(7m, null) }).IsValid.ShouldBeTrue();
        validator.Validate(Accepted() with { Rating = new RatingRange(null, 7m) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Rating_EmptyRange_IsValid()
    {
        validator.Validate(Accepted() with { Rating = new RatingRange(null, null) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Rating_Absent_IsValid()
    {
        validator.Validate(Accepted() with { Rating = null }).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(7.1, true)]
    [InlineData(10, true)]
    [InlineData(-0.1, false)]
    [InlineData(10.1, false)]
    [InlineData(7.55, false)]
    [InlineData(7.05, false)]
    public void Rating_MinBoundNumericDomain_MatchesImdbRatingValidity(double bound, bool valid)
    {
        var rating = new RatingRange((decimal)bound, null);

        validator.Validate(Accepted() with { Rating = rating }).IsValid.ShouldBe(valid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(7.1, true)]
    [InlineData(10, true)]
    [InlineData(-0.1, false)]
    [InlineData(10.1, false)]
    [InlineData(7.55, false)]
    public void Rating_MaxBoundNumericDomain_MatchesImdbRatingValidity(double bound, bool valid)
    {
        var rating = new RatingRange(null, (decimal)bound);

        validator.Validate(Accepted() with { Rating = rating }).IsValid.ShouldBe(valid);
    }

    private static MovieQuery Accepted() => new()
    {
        Sort = MovieSort.Title,
        Direction = SortDirection.Ascending,
        Page = new Page(1, 20),
    };
}
