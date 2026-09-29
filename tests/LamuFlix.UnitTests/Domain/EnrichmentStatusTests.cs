using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class EnrichmentStatusTests
{
    [Theory]
    [InlineData("Pending", 0)]
    [InlineData("Enriched", 1)]
    [InlineData("NotFound", 2)]
    [InlineData("Failed", 3)]
    public void Values_ArePinned(string name, int value)
    {
        EnrichmentStatus.TryFromName(name, false, out var status).ShouldBeTrue();
        status.ShouldNotBeNull();
        status.Value.ShouldBe(value);
        status.Name.ShouldBe(name);
    }
}
