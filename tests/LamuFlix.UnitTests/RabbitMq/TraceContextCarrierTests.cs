using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Context.Propagation;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class TraceContextCarrierTests
{
    [Fact]
    public void DefaultPropagator_AfterRegistration_IsTheW3CPropagator()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        Propagators.DefaultTextMapPropagator.ShouldBeOfType<TraceContextPropagator>();
    }

    [Fact]
    public void Inject_WithoutAnyActivity_StillWritesAValidTraceParent()
    {
        // arrange
        var headers = new Dictionary<string, object?>();

        // act
        TraceContextCarrier.Inject(headers);

        // assert
        var traceParent = Read(headers, TraceContextCarrier.TraceParentHeader);
        traceParent.ShouldStartWith("00-");
        ActivityTraceId.CreateFromString(traceParent.AsSpan(3, 32)).ToHexString().ShouldNotBeEmpty();
        ActivitySpanId.CreateFromString(traceParent.AsSpan(36, 16)).ToHexString().ShouldNotBeEmpty();
    }

    [Fact]
    public void Inject_WithoutATraceState_WritesNoTraceState()
    {
        // arrange
        var headers = new Dictionary<string, object?>();

        // act
        TraceContextCarrier.Inject(headers);

        // assert
        headers.ShouldNotContainKey(TraceContextCarrier.TraceStateHeader);
    }

    [Fact]
    public void Inject_WithAnAmbientActivity_UsesThatContext()
    {
        // arrange
        var headers = new Dictionary<string, object?>();
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(ActivitySourceName);
        using var activity = source.StartActivity(
            "publish",
            ActivityKind.Producer,
            new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded));
        activity.ShouldNotBeNull();

        // act
        TraceContextCarrier.Inject(headers);

        // assert
        Extract(headers).TraceId.ShouldBe(traceId);
        Extract(headers).SpanId.ShouldBe(activity.SpanId);
    }

    [Fact]
    public void Inject_WithATraceState_WritesBothHeaders()
    {
        // arrange
        var headers = new Dictionary<string, object?>();
        var context = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded,
            traceState: "vendor=value",
            isRemote: true);

        // act
        TraceContextCarrier.Inject(headers, context);

        // assert
        Read(headers, TraceContextCarrier.TraceStateHeader).ShouldContain("vendor=value");
        Read(headers, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
        Extract(headers).TraceState?.ToString().ShouldContain("vendor=value");
    }

    [Fact]
    public void Extract_InjectedHeaders_PreservesTheTraceId()
    {
        // arrange
        var headers = new Dictionary<string, object?>();
        var traceId = ActivityTraceId.CreateRandom();
        var context = new ActivityContext(
            traceId,
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);

        // act
        TraceContextCarrier.Inject(headers, context);

        // assert
        Extract(headers).TraceId.ShouldBe(traceId);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void InjectThenExtract_KeepsTheTraceIdForAnyTraceId()
    {
        Prop.ForAll<int>(seed =>
        {
            // arrange
            var traceId = TraceIdFrom(seed);
            var headers = new Dictionary<string, object?>();
            var context = new ActivityContext(
                traceId,
                ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.Recorded);

            // act
            TraceContextCarrier.Inject(headers, context);

            // assert
            return Extract(headers).TraceId == traceId;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Extract_WithoutATraceParent_ReturnsAnEmptyContext()
    {
        // arrange
        var headers = new Dictionary<string, object?> { ["content-type"] = "application/json" };

        // act
        var extracted = TraceContextCarrier.Extract(headers);

        // assert
        extracted.ActivityContext.TraceId.ShouldBe(default);
    }

    [Fact]
    public void Extract_WithNullHeaders_ReturnsAnEmptyContext()
    {
        // arrange
        IDictionary<string, object?>? headers = null;

        // act
        var extracted = TraceContextCarrier.Extract(headers);

        // assert
        extracted.ActivityContext.TraceId.ShouldBe(default);
    }

    [Fact]
    public void Inject_WithNullHeaders_DoesNothing()
    {
        // arrange
        IDictionary<string, object?>? headers = null;

        // act
        TraceContextCarrier.Inject(headers);

        // assert
        headers.ShouldBeNull();
    }

    [Fact]
    public void TryReadHeader_PresentHeader_ReturnsItsText()
    {
        // arrange
        var headers = new Dictionary<string, object?>
        {
            [TraceContextCarrier.TraceParentHeader] = Encoding.UTF8.GetBytes("value"),
        };

        // act
        var found = TraceContextCarrier.TryReadHeader(
            headers,
            TraceContextCarrier.TraceParentHeader,
            out var value);

        // assert
        found.ShouldBeTrue();
        value.ShouldBe("value");
    }

    [Fact]
    public void TryReadHeader_MissingHeader_ReturnsFalse()
    {
        // arrange
        var headers = new Dictionary<string, object?>();

        // act
        var found = TraceContextCarrier.TryReadHeader(headers, TraceContextCarrier.TraceParentHeader, out var value);

        // assert
        found.ShouldBeFalse();
        value.ShouldBeEmpty();
    }

    private const string ActivitySourceName = "carrier-test";

    private static ActivityTraceId TraceIdFrom(int seed)
    {
        Span<byte> bytes = stackalloc byte[16];
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(seed >> ((index % 4) * 8));
        }

        bytes[0] = 1;
        return ActivityTraceId.CreateFromBytes(bytes);
    }

    private static ActivityContext Extract(IDictionary<string, object?> headers) =>
        TraceContextCarrier.Extract(headers).ActivityContext;

    private static string Read(IDictionary<string, object?> headers, string name) =>
        TraceContextCarrier.TryReadHeader(headers, name, out var value) ? value : string.Empty;
}
