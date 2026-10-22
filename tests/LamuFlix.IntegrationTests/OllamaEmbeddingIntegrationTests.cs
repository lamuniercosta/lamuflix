using System;
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

/// <summary>
/// Opt-in proof that the merged DEV-412 adapter returns a real 384-dimension
/// embedding from a real Ollama model. Skipped before any container, model, or
/// resource object exists unless <see cref="OllamaFixture.OptInVariable"/> is set.
/// </summary>
[Trait("Category", "OllamaIntegration")]
public sealed class OllamaEmbeddingIntegrationTests(OllamaFixture fixture)
    : IClassFixture<OllamaFixture>
{
    private const string SamplePlot =
        "A botanist follows a signal from an abandoned observatory and discovers a hidden garden.";

    [Fact]
    public async Task GenerateAsync_SamplePlot_Returns384DimensionalEmbedding()
    {
        // arrange: defence-in-depth gate; the fixture already refused to start without opt-in.
        Assert.SkipUnless(OllamaFixture.IsOptedIn, OllamaFixture.SkipReason);

        await using var provider = fixture.BuildServices();

        // act: resolve through the real registration and run its validated startup seam.
        var validator = provider.GetServices<IHostedService>()
            .OfType<OllamaEmbeddingStartupValidator>()
            .Single();
        await validator.StartAsync(TestContext.Current.CancellationToken);

        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        deadline.CancelAfter(OllamaFixture.GenerationBudget);

        var result = await generator.GenerateAsync([SamplePlot], cancellationToken: deadline.Token);

        // assert: exactly one embedding, whose length is the native model dimension.
        result.Count.ShouldBe(1);
        result[0].Vector.Length.ShouldBe(EmbeddingDimensions.Value);
    }
}
