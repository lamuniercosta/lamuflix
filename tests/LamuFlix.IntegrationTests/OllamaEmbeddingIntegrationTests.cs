using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Trait("Category", "OllamaIntegration")]
public sealed class OllamaEmbeddingIntegrationTests(OllamaFixture fixture)
    : IClassFixture<OllamaFixture>
{
    private const string SamplePlot =
        "A botanist follows a signal from an abandoned observatory and discovers a hidden garden.";

    [Fact]
    public async Task GenerateAsync_SamplePlot_Returns384DimensionalEmbedding()
    {
        // arrange
        Assert.SkipUnless(OllamaFixture.IsOptedIn, OllamaFixture.SkipReason);

        await using var provider = fixture.BuildServices();

        // act
        var validator = provider.GetServices<IHostedService>()
            .OfType<OllamaEmbeddingStartupValidator>()
            .Single();
        await validator.StartAsync(TestContext.Current.CancellationToken);

        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        deadline.CancelAfter(OllamaFixture.GenerationBudget);

        var result = await generator.GenerateAsync([SamplePlot], cancellationToken: deadline.Token);

        // assert
        result.Count.ShouldBe(1);
        result[0].Vector.Length.ShouldBe(EmbeddingDimensions.Value);
    }
}
