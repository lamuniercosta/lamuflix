using System;
using System.Collections.Generic;
using FsCheck;
using LamuFlix.Core.Domain;
using Xunit;

namespace LamuFlix.Test.Domain;

public sealed class PropertyTests
{
    [Fact]
    [Trait("Category", "Property")]
    public void MovieId_TryCreate_AgreesWithPositiveRule()
    {
        Check.QuickThrowOnFailure((int value) => TryAgrees(
            MovieId.TryCreate(value, out var created),
            created,
            value > 0,
            () => new MovieId(value)));
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbId_TryCreate_AgreesWithPattern()
    {
        Check.QuickThrowOnFailure((string? value) =>
        {
            var matches = value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, @"^tt\d{7,8}$");
            return TryAgrees(ImdbId.TryCreate(value, out var created), created, matches, () => new ImdbId(value));
        });
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbRating_TryCreate_AgreesWithOneDecimalRule()
    {
        Check.QuickThrowOnFailure((decimal value) =>
        {
            var valid = value >= 0.0m && value <= 10.0m && value == decimal.Round(value, 1);
            return TryAgrees(ImdbRating.TryCreate(value, out var created), created, valid, () => new ImdbRating(value));
        });
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Runtime_TryCreate_AgreesWithPositiveRule()
    {
        Check.QuickThrowOnFailure((int minutes) => TryAgrees(
            Runtime.TryCreate(minutes, out var created),
            created,
            minutes > 0,
            () => new Runtime(minutes)));
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ReleaseYear_TryCreate_AgreesWithBounds()
    {
        Check.QuickThrowOnFailure((int value, DateTimeOffset now) =>
        {
            var valid = value >= 1888 && value <= now.Year + 5;
            return TryAgrees(
                ReleaseYear.TryCreate(value, now, out var created),
                created,
                valid,
                () => new ReleaseYear(value, now));
        });
    }

    [Fact]
    [Trait("Category", "Property")]
    public void LibraryPath_TryCreate_RejectsBlankAndParentSegments()
    {
        Check.QuickThrowOnFailure((string? value) =>
        {
            var valid = !string.IsNullOrWhiteSpace(value) && !value.Contains("..", StringComparison.Ordinal);
            return TryAgrees(LibraryPath.TryCreate(value, out var created), created, valid, () => new LibraryPath(value));
        });
    }

    [Fact]
    [Trait("Category", "Property")]
    public void MediaFormat_TryCreate_AgreesWithNormalization()
    {
        Check.QuickThrowOnFailure((string? value) =>
        {
            var valid = TryNormalize(value, out var expected);
            var createdOk = MediaFormat.TryCreate(value, out var created);
            if (createdOk != valid)
            {
                return false;
            }

            if (!valid)
            {
                return created is null && ConstructorThrows(value);
            }

            return created is not null
                && created.Extension == expected
                && new MediaFormat(value).Extension == expected;
        });
    }

    private static bool TryAgrees<T>(bool createdOk, T? created, bool valid, Func<T> construct)
        where T : class
    {
        if (createdOk != valid)
        {
            return false;
        }

        if (!valid)
        {
            return created is null && ConstructorThrows(construct);
        }

        return created is not null && EqualityComparer<T>.Default.Equals(created, construct());
    }

    private static bool ConstructorThrows<T>(Func<T> construct)
    {
        try
        {
            _ = construct();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static bool ConstructorThrows(string? value)
    {
        try
        {
            _ = new MediaFormat(value);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static bool TryNormalize(string? extension, out string expected)
    {
        expected = string.Empty;
        if (extension is null)
        {
            return false;
        }

        var trimmed = extension.Trim();
        if (trimmed.StartsWith('.'))
        {
            trimmed = trimmed[1..];
        }

        trimmed = trimmed.ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return false;
        }

        expected = trimmed;
        return true;
    }
}
