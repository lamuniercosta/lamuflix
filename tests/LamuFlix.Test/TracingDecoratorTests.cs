using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Pipeline;
using Shouldly;
using Xunit;

namespace LamuFlix.Test;

public sealed class TracingDecoratorTests
{
    [Fact]
    public async Task HandleAsync_ListenerPresent_StartsChildActivityWithRequestAttribute()
    {
        // arrange
        var source = new ActivitySource(nameof(HandleAsync_ListenerPresent_StartsChildActivityWithRequestAttribute));
        Activity? observed = null;
        using var listener = Listen(source, activity => observed = activity);
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => Task.FromResult("ok"),
            source);
        using var parent = new Activity("parent");
        parent.Start();

        // act
        string result;
        try
        {
            result = await decorator.HandleAsync(new SampleRequest(), CancellationToken.None);
        }
        finally
        {
            parent.Stop();
        }

        // assert
        result.ShouldBe("ok");
        observed.ShouldNotBeNull();
        observed.DisplayName.ShouldBe(nameof(SampleRequest));
        observed.ParentId.ShouldBe(parent.Id);
        observed.GetTagItem(TelemetryConstants.HandlerRequest).ShouldBe(nameof(SampleRequest));
    }

    [Fact]
    public async Task HandleAsync_HandlerThrows_SetsErrorStatusAndRethrows()
    {
        // arrange
        var source = new ActivitySource(nameof(HandleAsync_HandlerThrows_SetsErrorStatusAndRethrows));
        Activity? observed = null;
        using var listener = Listen(source, activity => observed = activity);
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => throw new InvalidOperationException("boom"),
            source);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => decorator.HandleAsync(new SampleRequest(), CancellationToken.None));

        // assert
        exception.Message.ShouldBe("boom");
        observed.ShouldNotBeNull();
        observed.Status.ShouldBe(ActivityStatusCode.Error);
        observed.GetTagItem(TelemetryConstants.ErrorType).ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task HandleAsync_NoListener_ReturnsResult()
    {
        // arrange
        var source = new ActivitySource(nameof(HandleAsync_NoListener_ReturnsResult));
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => Task.FromResult("ok"),
            source);

        // act
        var result = await decorator.HandleAsync(new SampleRequest(), CancellationToken.None);

        // assert
        result.ShouldBe("ok");
    }

    [Fact]
    public async Task HandleAsync_NoListener_RethrowsException()
    {
        // arrange
        var source = new ActivitySource(nameof(HandleAsync_NoListener_RethrowsException));
        var decorator = new TracingDecorator<SampleRequest, string>(
            (_, _) => throw new InvalidOperationException("boom"),
            source);

        // act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => decorator.HandleAsync(new SampleRequest(), CancellationToken.None));

        // assert
        exception.Message.ShouldBe("boom");
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
