using System;
using System.Runtime.CompilerServices;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace LamuFlix.IntegrationTests;

/// The application installs the W3C propagator while building its container; tests that build their
/// own RabbitMQ host do the same so the default propagator is never a no-op.
internal static class TestTelemetrySetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());
    }
}