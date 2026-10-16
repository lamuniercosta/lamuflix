using System.Threading.Tasks;
using LamuFlix.Tests.Common;
using Xunit;

[assembly: AssemblyFixture(typeof(LamuFlix.Test.ContainerFixtureForTests))]

namespace LamuFlix.Test;

public sealed class ContainerFixtureForTests : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(ContainerFixture.StartAsync());

    public ValueTask DisposeAsync() => ContainerFixture.DisposeAsync();
}
