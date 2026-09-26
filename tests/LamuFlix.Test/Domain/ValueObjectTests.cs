using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using LamuFlix.Core.Domain;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Domain;

public sealed class ValueObjectTests
{
    private static readonly DateTimeOffset Now = new(2020, 6, 15, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void MovieId_TryCreate_MatchesRule(int value, bool expected)
    {
        MovieId.TryCreate(value, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            new MovieId(value).Value.ShouldBe(value);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => new MovieId(value));
    }

    [Theory]
    [InlineData("tt1234567", true)]
    [InlineData("tt12345678", true)]
    [InlineData("tt123456", false)]
    [InlineData("tt123456789", false)]
    [InlineData("TT1234567", false)]
    [InlineData("", false)]
    public void ImdbId_TryCreate_MatchesRule(string value, bool expected)
    {
        ImdbId.TryCreate(value, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            new ImdbId(value).Value.ShouldBe(value);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentException>(() => new ImdbId(value));
    }

    [Theory]
    [InlineData("0.0", true)]
    [InlineData("10.0", true)]
    [InlineData("5.5", true)]
    [InlineData("5.50", true)]
    [InlineData("10.00", true)]
    [InlineData("10.1", false)]
    [InlineData("-0.1", false)]
    [InlineData("5.55", false)]
    [InlineData("10.05", false)]
    public void ImdbRating_TryCreate_MatchesRule(string literal, bool expected)
    {
        var value = decimal.Parse(literal, CultureInfo.InvariantCulture);
        ImdbRating.TryCreate(value, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            new ImdbRating(value).Value.ShouldBe(value);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => new ImdbRating(value));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Runtime_TryCreate_MatchesRule(int minutes, bool expected)
    {
        Runtime.TryCreate(minutes, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Minutes.ShouldBe(minutes);
            new Runtime(minutes).Minutes.ShouldBe(minutes);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => new Runtime(minutes));
    }

    [Theory]
    [InlineData(1888, true)]
    [InlineData(2025, true)]
    [InlineData(1887, false)]
    [InlineData(2026, false)]
    public void ReleaseYear_TryCreate_MatchesRule(int value, bool expected)
    {
        ReleaseYear.TryCreate(value, Now, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            new ReleaseYear(value, Now).Value.ShouldBe(value);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => new ReleaseYear(value, Now));
    }

    [Fact]
    public void ReleaseYear_EqualValues_IgnoreSuppliedNow()
    {
        var earlier = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var left = new ReleaseYear(1999, earlier);
        var right = new ReleaseYear(1999, later);

        left.ShouldBe(right);
        new ReleaseYear(2000, earlier).ShouldNotBe(left);
    }

    [Theory]
    [InlineData("C:/library/inception", true)]
    [InlineData("folder", true)]
    [InlineData("..", false)]
    [InlineData("a/../b", false)]
    [InlineData("a..b", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void LibraryPath_TryCreate_MatchesRule(string value, bool expected)
    {
        LibraryPath.TryCreate(value, out var result).ShouldBe(expected);
        if (expected)
        {
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            new LibraryPath(value).Value.ShouldBe(value);
            return;
        }

        result.ShouldBeNull();
        Should.Throw<ArgumentException>(() => new LibraryPath(value));
    }

    [Theory]
    [InlineData(" MKV ", "mkv")]
    [InlineData(".MKV", "mkv")]
    [InlineData("..mkv", ".mkv")]
    [InlineData("mkv", "mkv")]
    [InlineData(".", null)]
    [InlineData(" . ", null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void MediaFormat_TryCreate_NormalizesOrRejects(string value, string? expected)
    {
        var created = MediaFormat.TryCreate(value, out var result);
        if (expected is null)
        {
            created.ShouldBeFalse();
            result.ShouldBeNull();
            Should.Throw<ArgumentException>(() => new MediaFormat(value));
            return;
        }

        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Extension.ShouldBe(expected);
        new MediaFormat(value).Extension.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void MovieMetadata_BlankTitle_Throws(string title)
    {
        Should.Throw<ArgumentException>(() => new MovieMetadata(title));
    }

    [Fact]
    public void ValueObjects_Properties_HaveNoSetter()
    {
        foreach (var type in ValueObjectTypes())
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                property.SetMethod.ShouldBeNull();
            }
        }
    }

    private static IEnumerable<Type> ValueObjectTypes()
    {
        yield return typeof(MovieId);
        yield return typeof(ImdbId);
        yield return typeof(ImdbRating);
        yield return typeof(Runtime);
        yield return typeof(ReleaseYear);
        yield return typeof(LibraryPath);
        yield return typeof(MediaFormat);
        yield return typeof(MovieMetadata);
    }
}
