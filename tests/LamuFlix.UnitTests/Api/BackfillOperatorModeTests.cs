using System;
using LamuFlix.Api;
using Xunit;

namespace LamuFlix.UnitTests.Api;

/// <summary>
/// Pins the operator invocation contract: normal host arguments stay untouched, the exact flag is the only
/// valid operator invocation, and any extra or altered argument is a malformed operator invocation.
/// </summary>
public sealed class BackfillOperatorModeTests
{
    public static TheoryData<string[]> NormalHostArguments => new()
    {
        Array.Empty<string>(),
        new[] { "--environment", "Development" },
        new[] { "--urls", "http://localhost:5000" },
    };

    public static TheoryData<string[]> MalformedOperatorArguments => new()
    {
        new[] { "--backfill-tmdb-recommendations", "--environment", "Development" },
        new[] { "--backfill-tmdb-recommendations=true" },
        new[] { "--backfill-tmdb-recommendations", "--backfill-tmdb-recommendations" },
        new[] { "--backfill-tmdb-recommendations", "extra" },
        Array.Empty<string>(),
    };

    public static TheoryData<string[]> OperatorFlagArguments => new()
    {
        new[] { "--backfill-tmdb-recommendations" },
        new[] { "--backfill-tmdb-recommendations", "--environment", "Development" },
        new[] { "--backfill-tmdb-recommendations=true" },
        new[] { "--backfill-tmdb-recommendations", "--backfill-tmdb-recommendations" },
    };

    [Theory]
    [MemberData(nameof(NormalHostArguments))]
    public void IsRequested_NormalHostArguments_IsFalse(string[] args)
    {
        BackfillOperatorMode.IsRequested(args).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(OperatorFlagArguments))]
    public void IsRequested_OperatorFlagOrMalformedSpelling_IsTrue(string[] args)
    {
        BackfillOperatorMode.IsRequested(args).ShouldBeTrue();
    }

    [Fact]
    public void TryParse_ExactFlag_IsAccepted()
    {
        BackfillOperatorMode.TryParse([BackfillOperatorMode.Flag], out var error).ShouldBeTrue();
        error.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(MalformedOperatorArguments))]
    public void TryParse_MalformedOrAbsentFlag_IsRejectedWithASanitizedMessage(string[] args)
    {
        BackfillOperatorMode.TryParse(args, out var error).ShouldBeFalse();
        error.ShouldNotBeNull();
        error.ShouldNotContain("api_key");
        error.ShouldNotContain("ApiKey");
    }
}
