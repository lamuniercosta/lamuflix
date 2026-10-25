using System;
using System.Collections.Generic;
using System.IO;
using LamuFlix.Evaluation.Entry;
using LamuFlix.Evaluation.Output;
using LamuFlix.Evaluation.Tests.TestSupport;

namespace LamuFlix.Evaluation.Tests.Entry;

/// <summary>
/// T014/T015 — publication is intentional. The mode marker plus validated
/// transported parameters are required before anything is written, so ordinary
/// suite execution of the dedicated entry never publishes (Mode A, TB002).
/// </summary>
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
            [EvaluationEntry.FixturePathVariable] = EvaluationCommand.FixturePath,
            [EvaluationEntry.OutputPathVariable] = output,
            [EvaluationEntry.SeedVariable] = "356",
        };
}
