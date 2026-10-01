using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ReleaseYearComparisonTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompareToAndOperators_OrderValuesAndHandleNull()
    {
        ReleaseYear? lower = new(1990, Now);
        ReleaseYear? higher = new(2020, Now);
        ReleaseYear? missing = null;
        ReleaseYear? alsoMissing = null;

        lower.CompareTo(higher).ShouldBeLessThan(0);
        higher.CompareTo(lower).ShouldBeGreaterThan(0);
        lower.CompareTo(new ReleaseYear(1990, Now)).ShouldBe(0);
        lower.CompareTo(null).ShouldBeGreaterThan(0);
        (lower < higher).ShouldBeTrue();
        (lower <= new ReleaseYear(1990, Now)).ShouldBeTrue();
        (higher > lower).ShouldBeTrue();
        (higher >= new ReleaseYear(2020, Now)).ShouldBeTrue();
        (missing < lower).ShouldBeTrue();
        (missing <= alsoMissing).ShouldBeTrue();
        (lower > missing).ShouldBeTrue();
        (lower >= missing).ShouldBeTrue();
        (missing < alsoMissing).ShouldBeFalse();
        (missing <= lower).ShouldBeTrue();
        (missing > lower).ShouldBeFalse();
        (missing >= lower).ShouldBeFalse();
        (missing >= alsoMissing).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Comparisons_AgreeWithIntegerOrdering()
    {
        Prop.ForAll<int, int>((left, right) =>
        {
            ReleaseYear lower = new(1888 + Math.Abs(left % 138), Now);
            ReleaseYear higher = new(1888 + Math.Abs(right % 138), Now);
            ReleaseYear? missing = null;
            ReleaseYear? alsoMissing = null;
            var comparison = lower.Value.CompareTo(higher.Value);

            return lower.CompareTo(higher) == comparison
                && (lower < higher) == (comparison < 0)
                && (lower <= higher) == (comparison <= 0)
                && (lower > higher) == (comparison > 0)
                && (lower >= higher) == (comparison >= 0)
                && (missing < lower)
                && (lower > missing)
                && (missing <= alsoMissing)
                && (missing >= alsoMissing)
                && !(missing > lower)
                && !(missing >= lower);
        }).QuickCheckThrowOnFailure();
    }
}