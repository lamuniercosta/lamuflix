using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace LamuFlix.Infrastructure.RabbitMq;

public static class TraceContextCarrier
{
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader = "tracestate";

    public static void Inject(IDictionary<string, object?>? headers, ActivityContext? context = null)
    {
        if (headers is null)
        {
            return;
        }

        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(context ?? Activity.Current?.Context ?? NewContext(), Baggage.Current),
            headers,
            static (target, key, value) => target[key] = Encoding.UTF8.GetBytes(value));
    }

    public static PropagationContext Extract(IDictionary<string, object?>? headers) =>
        headers is null
            ? default
            : Propagators.DefaultTextMapPropagator.Extract(
                default,
                headers,
                static (source, key) => TryReadHeader(source, key, out var value) ? [value] : null);

    public static ActivityContext ExtractContext(IDictionary<string, object?>? headers) =>
        Extract(headers).ActivityContext;

    public static bool TryReadHeader(IDictionary<string, object?>? headers, string name, out string value)
    {
        value = string.Empty;
        if (headers is null || !headers.TryGetValue(name, out var raw))
        {
            return false;
        }

        value = raw switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => raw?.ToString() ?? string.Empty,
        };
        return value.Length > 0;
    }

    private static ActivityContext NewContext() =>
        new(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);
}