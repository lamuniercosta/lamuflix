using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Evaluation.RealLibrary;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.Entry;

public sealed class EvaluationEntryModeTests
{
    [Fact]
    public void IsRequested_IsFalseWhenTheMarkerIsAbsentOrDifferent()
    {
        EvaluationEntry.IsRequested(_ => null).ShouldBeFalse();
        EvaluationEntry.IsRequested(name => name == EvaluationEntry.ModeVariable ? "other" : null)
            .ShouldBeFalse();
    }

    [Fact]
    public void IsRequested_IsTrueForTheRuledMarkerValue()
    {
        EvaluationEntry.IsRequested(
                name => name == EvaluationEntry.ModeVariable ? EvaluationEntry.ModeValue : null)
            .ShouldBeTrue();
    }

    [Fact]
    public void Publish_WithoutTheMarker_WritesNothingEvenWithValidParameters()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");
        var environment = Parameters(output);

        // act
        var published = EvaluationEntry.Publish(name => environment.GetValueOrDefault(name));

        // assert
        published.ShouldBeNull();
        File.Exists(output).ShouldBeFalse();
    }

    [Fact]
    public void Publish_WithTheMarker_WritesTheBaselineSummary()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");
        var environment = Parameters(output);
        environment[EvaluationEntry.ModeVariable] = EvaluationEntry.ModeValue;

        // act
        var published = EvaluationEntry.Publish(name => environment.GetValueOrDefault(name));

        // assert
        published.ShouldBe(output);
        File.Exists(output).ShouldBeTrue();
        File.ReadAllText(output).ShouldStartWith("{\"schemaVersion\":1,\"fixtureId\":\"dev-356-baseline\"");
    }

    [Fact]
    public void Publish_WithTheMarker_RequiresTheTransportedParameters()
    {
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");

        foreach (var missing in new[]
                 {
                     EvaluationEntry.FixturePathVariable,
                     EvaluationEntry.OutputPathVariable,
                     EvaluationEntry.SeedVariable,
                 })
        {
            var environment = Parameters(output);
            environment[EvaluationEntry.ModeVariable] = EvaluationEntry.ModeValue;
            environment.Remove(missing);

            var failure = Should.Throw<InvalidOperationException>(
                () => EvaluationEntry.Publish(name => environment.GetValueOrDefault(name)));
            failure.Message.ShouldContain(missing);
        }

        File.Exists(output).ShouldBeFalse();
    }

    [Fact]
    public void Publish_WithTheMarkerAndMissingFixtureFile_FailsWithoutPublishing()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var output = directory.Combine("summary.json");
        var environment = Parameters(output);
        environment[EvaluationEntry.ModeVariable] = EvaluationEntry.ModeValue;
        environment[EvaluationEntry.FixturePathVariable] = directory.Combine("absent.json");

        // act / assert
        Should.Throw<FileNotFoundException>(
            () => EvaluationEntry.Publish(name => environment.GetValueOrDefault(name)));
        File.Exists(output).ShouldBeFalse();
    }

    private static Dictionary<string, string?> Parameters(string output) =>
        new(StringComparer.Ordinal)
        {
            [EvaluationEntry.FixturePathVariable] = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline.json"),
            [EvaluationEntry.OutputPathVariable] = output,
            [EvaluationEntry.SeedVariable] = "356",
        };

    [Fact]
    public void IsRealLibraryRequested_IsFalseWithoutTheMarkerOrForPublish()
    {
        EvaluationEntry.IsRealLibraryRequested(_ => null).ShouldBeFalse();
        EvaluationEntry.IsRealLibraryRequested(
                name => name == EvaluationEntry.ModeVariable ? EvaluationEntry.ModeValue : null)
            .ShouldBeFalse();
        EvaluationEntry.IsRealLibraryRequested(
                name => name == EvaluationEntry.ModeVariable ? "other" : null)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(EvaluationEntry.LeaveOneOutModeValue)]
    [InlineData(EvaluationEntry.CompareModeValue)]
    [InlineData(EvaluationEntry.QualityModeValue)]
    public void IsRealLibraryRequested_IsTrueOnlyForTheRuledRealLibraryModes(string mode)
    {
        EvaluationEntry.IsRealLibraryRequested(
                name => name == EvaluationEntry.ModeVariable ? mode : null)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task RunRealLibraryAsync_WithoutTheMarker_ReturnsNullAndTouchesNothing()
    {
        // arrange
        var reader = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.SelectedIdVariable] = "1",
            [RealLibrarySnapshotLoader.CredentialVariableName] = "Host=unused",
        };

        // act
        var output = await EvaluationEntry.RunRealLibraryAsync(
            name => reader.GetValueOrDefault(name),
            CancellationToken.None);

        // assert
        output.ShouldBeNull();
    }

    [Theory]
    [InlineData(EvaluationEntry.LeaveOneOutModeValue)]
    [InlineData(EvaluationEntry.CompareModeValue)]
    [InlineData(EvaluationEntry.QualityModeValue)]
    public async Task RunRealLibraryAsync_RequiresTheCredentialWithoutEchoingIt(string mode)
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var reader = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = mode,
            [EvaluationEntry.SelectedIdVariable] = "1",
            [EvaluationEntry.OutputPathVariable] = directory.Combine("report.json"),
        };

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => EvaluationEntry.RunRealLibraryAsync(
                name => reader.GetValueOrDefault(name),
                CancellationToken.None));

        // assert
        failure.Message.ShouldContain(RealLibrarySnapshotLoader.CredentialVariableName);
        failure.Message.ShouldNotContain("Host=");
        File.Exists(reader[EvaluationEntry.OutputPathVariable]).ShouldBeFalse();
    }

    [Fact]
    public async Task RunRealLibraryAsync_CompareRequiresAPositiveSelectedId()
    {
        // arrange
        var reader = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.CompareModeValue,
            [RealLibrarySnapshotLoader.CredentialVariableName] = "Host=unused",
        };

        // act / assert
        var missing = await Should.ThrowAsync<InvalidOperationException>(
            () => EvaluationEntry.RunRealLibraryAsync(name => reader.GetValueOrDefault(name), CancellationToken.None));
        missing.Message.ShouldContain(EvaluationEntry.SelectedIdVariable);

        foreach (var invalid in new[] { "0", "-3", "not-a-number" })
        {
            reader[EvaluationEntry.SelectedIdVariable] = invalid;

            var failure = await Should.ThrowAsync<InvalidOperationException>(
                () => EvaluationEntry.RunRealLibraryAsync(name => reader.GetValueOrDefault(name), CancellationToken.None));
            failure.Message.ShouldContain(EvaluationEntry.SelectedIdVariable);
            failure.Message.ShouldNotContain(RealLibrarySnapshotLoader.CredentialVariableName + " =");
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4")]
    [InlineData("abc")]
    public async Task RunRealLibraryAsync_CompareRejectsAnInvalidOtherId(string otherId)
    {
        // arrange
        var reader = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.CompareModeValue,
            [EvaluationEntry.SelectedIdVariable] = "2",
            [EvaluationEntry.OtherIdVariable] = otherId,
            [RealLibrarySnapshotLoader.CredentialVariableName] = "Host=unused",
        };

        // act / assert
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => EvaluationEntry.RunRealLibraryAsync(name => reader.GetValueOrDefault(name), CancellationToken.None));
        failure.Message.ShouldContain(EvaluationEntry.OtherIdVariable);
    }

    [Fact]
    public void RealLibraryModeScope_RestoresEveryPriorMarkerOnDispose()
    {
        // arrange
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.ModeValue,
            [EvaluationEntry.OutputPathVariable] = "prior.json",
        };

        var markers = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.LeaveOneOutModeValue,
            [EvaluationEntry.OutputPathVariable] = "artifacts/recommendations/leave-one-out.json",
            [EvaluationEntry.SelectedIdVariable] = "101",
        };

        // act
        using (new RealLibraryModeScope(
                   name => environment.GetValueOrDefault(name),
                   (name, value) => environment[name] = value,
                   markers))
        {
            // assert within the scope
            environment[EvaluationEntry.ModeVariable].ShouldBe(EvaluationEntry.LeaveOneOutModeValue);
            environment[EvaluationEntry.SelectedIdVariable].ShouldBe("101");
        }

        // assert after the scope
        environment[EvaluationEntry.ModeVariable].ShouldBe(EvaluationEntry.ModeValue);
        environment[EvaluationEntry.OutputPathVariable].ShouldBe("prior.json");
        environment.ContainsKey(EvaluationEntry.SelectedIdVariable).ShouldBeTrue();
        environment[EvaluationEntry.SelectedIdVariable].ShouldBeNull();
    }

    [Fact]
    public void RealLibraryModeScope_RemovesMarkersThatWereAbsentBefore()
    {
        // arrange
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal);

        // act
        using (new RealLibraryModeScope(
                   name => environment.GetValueOrDefault(name),
                   (name, value) => environment[name] = value,
                   new Dictionary<string, string?>(StringComparer.Ordinal)
                   {
                       [EvaluationEntry.ModeVariable] = EvaluationEntry.CompareModeValue,
                   }))
        {
            environment[EvaluationEntry.ModeVariable].ShouldBe(EvaluationEntry.CompareModeValue);
        }

        // assert
        environment[EvaluationEntry.ModeVariable].ShouldBeNull();
    }
}
