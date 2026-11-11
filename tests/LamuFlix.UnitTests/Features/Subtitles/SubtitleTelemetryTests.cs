using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Shouldly;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleTelemetryTests
{
    [Fact]
    public void StartPrepare_StartsAnActivityNamedForThePrepareOperation()
    {
        // arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        // act
        using var activity = SubtitleTelemetry.StartPrepare();

        // assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(TelemetryConstants.SubtitlePrepare);
    }

    [Fact]
    public void StartServeAndLookup_UseTheFunctionalityNamedOperations()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);

        // act
        using var serve = SubtitleTelemetry.StartServe();
        using var lookup = SubtitleTelemetry.StartProviderLookup();

        // assert
        serve.ShouldNotBeNull().OperationName.ShouldBe(TelemetryConstants.SubtitleServe);
        lookup.ShouldNotBeNull().OperationName.ShouldBe(TelemetryConstants.SubtitleProviderLookup);
    }

    [Fact]
    public void Complete_TagsTheMovieIdAndOutcome()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);
        using var activity = SubtitleTelemetry.StartPrepare();

        // act
        SubtitleTelemetry.Complete(activity, new MovieId(7), TelemetryConstants.SubtitleSavedOutcome, TimeSpan.FromMilliseconds(5));

        // assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(7);
        activity.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.SubtitleSavedOutcome);
    }

    [Fact]
    public void Fail_TagsTheFailureCategoryAndMarksTheActivityAsAnError()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);
        using var activity = SubtitleTelemetry.StartPrepare();

        // act
        SubtitleTelemetry.Fail(activity, new MovieId(7), new NotFoundException());

        // assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(TelemetryConstants.FailureCategoryTagName).ShouldBe(TelemetryConstants.NotFoundOutcome);
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void Complete_WithNullActivity_StillRecordsTheOutcomeMetric()
    {
        // act
        var act = () => SubtitleTelemetry.Complete(null, new MovieId(7), TelemetryConstants.SubtitleNoMatchOutcome, TimeSpan.Zero);

        // assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Complete_WithNullMovieId_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleTelemetry.Complete(null, null!, TelemetryConstants.SubtitleNoMatchOutcome, TimeSpan.Zero);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Fail_WithNullException_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleTelemetry.Fail(null, new MovieId(7), null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Complete_WithBlankOutcome_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleTelemetry.Complete(null, new MovieId(7), "  ", TimeSpan.Zero);

        // assert
        Should.Throw<ArgumentException>(act);
    }

    private static ActivityListener ListeningListener() =>
        new()
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
}
