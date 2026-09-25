using LamuFlix.ArchitectureTests.Fixtures.Violating.Features.UserManagement;

// ReSharper disable once CheckNamespace
namespace LamuFlix.ArchitectureTests.Fixtures.Violating.Features.MediaLibrary;

public sealed class DirectFeatureCouplingFixture
{
    public UserDirectory User { get; } = new();
}
