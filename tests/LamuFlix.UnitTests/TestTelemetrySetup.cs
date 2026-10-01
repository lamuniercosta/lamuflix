using System.Runtime.CompilerServices;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace LamuFlix.UnitTests;

/// The application installs the W3C propagator while building its container; tests do the same so the
/// default propagator is never a no-op. The assignment is idempotent, so the module runs it once.
internal static class TestTelemetrySetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());
    }
}
