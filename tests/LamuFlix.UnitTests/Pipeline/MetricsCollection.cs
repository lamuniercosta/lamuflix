namespace LamuFlix.UnitTests.Pipeline;

/// <summary>
/// Isolation collection for the unit tests that observe the application instruments.
/// Every observing test class carries exactly one <c>[Collection]</c> assignment to it, and
/// <c>DisableParallelization = true</c> keeps each MeterListener capture free of measurements
/// recorded by tests running beside it. The definition owns no fixture: fixtures stay declared
/// and owned by the approved test file that consumes them, not by this collection.
/// </summary>
[CollectionDefinition(nameof(MetricsCollection), DisableParallelization = true)]
public sealed class MetricsCollection;
