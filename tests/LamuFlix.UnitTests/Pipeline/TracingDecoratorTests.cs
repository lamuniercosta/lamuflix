using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Pipeline;

namespace LamuFlix.UnitTests.Pipeline;

public sealed class TracingDecoratorTests
{
    [Fact]
    public async Task HandleAsync_ValidationFailure_LeavesStatusUnsetAndSetsValidationFailedOutcome()
    {
        // arrange
        var source = new ActivitySource(
            nameof(HandleAsync_ValidationFailure_LeavesStatusUnsetAndSetsValidationFailedOutcome));
        Activity? observed = null;
        using var listener = Listen(source, activity => observed = activity);
        var thrown = new ValidationException(new Dictionary<string, string[]>());
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => throw thrown,
            source);

        // act
        var exception = await Should.ThrowAsync<ValidationException>(
            () => decorator.HandleAsync(new SampleRequest(), CancellationToken.None));

        // assert
        exception.ShouldBe(thrown);
        observed.ShouldNotBeNull();
        observed.Status.ShouldBe(ActivityStatusCode.Unset);
        observed.GetTagItem(TelemetryConstants.HandlerOutcome)
            .ShouldBe(TelemetryConstants.HandlerOutcomeValidationFailed);
    }

    [Fact]
    public async Task HandleAsync_InvalidOperationException_SetsErrorStatusAndFullTypeName()
    {
        // arrange
        var source = new ActivitySource(
            nameof(HandleAsync_InvalidOperationException_SetsErrorStatusAndFullTypeName));
        Activity? observed = null;
        using var listener = Listen(source, activity => observed = activity);
        var thrown = new InvalidOperationException("boom");
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => throw thrown,
            source);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => decorator.HandleAsync(new SampleRequest(), CancellationToken.None));

        // assert
        exception.ShouldBe(thrown);
        observed.ShouldNotBeNull();
        observed.Status.ShouldBe(ActivityStatusCode.Error);
        observed.GetTagItem(TelemetryConstants.ErrorType)
            .ShouldBe(typeof(InvalidOperationException).FullName);
    }

    private static ActivityListener Listen(ActivitySource source, Action<Activity> onStopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == source.Name,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = onStopped,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed record SampleRequest;
}
