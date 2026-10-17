using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LamuFlix.Core.Ports;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace LamuFlix.UnitTests;

public sealed class EmbeddingDimensionsTests
{
    [Fact]
    public void Validate_ReportedDimensionIs384_AcceptsWithoutGenerating()
    {
        var generator = CreateSubstituteWithMetadata(
            new EmbeddingGeneratorMetadata(defaultModelDimensions: EmbeddingDimensions.Value));

        Should.NotThrow(() => EmbeddingDimensions.Validate(generator));

        AssertNoGeneration(generator);
    }

    [Fact]
    public void Validate_DefaultFake_AcceptsFromReportedDimension()
    {
        var generator = new FakeEmbeddingGenerator();

        Should.NotThrow(() => EmbeddingDimensions.Validate(generator));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(383)]
    [InlineData(385)]
    [InlineData(768)]
    [InlineData(1536)]
    public void Validate_ReportedDimensionIsAnotherInteger_RejectsWithoutGenerating(int reportedDimensions)
    {
        var generator = CreateSubstituteWithMetadata(
            new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions));

        Should.Throw<InvalidOperationException>(() => EmbeddingDimensions.Validate(generator));

        AssertNoGeneration(generator);
    }

    [Fact]
    public void Validate_MetadataIsAbsent_RejectsWithoutGenerating()
    {
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        generator.GetService(typeof(EmbeddingGeneratorMetadata)).Returns((object?)null);

        Should.Throw<InvalidOperationException>(() => EmbeddingDimensions.Validate(generator));

        AssertNoGeneration(generator);
    }

    [Fact]
    public void Validate_ReportedDimensionIsNull_RejectsWithoutGenerating()
    {
        var generator = CreateSubstituteWithMetadata(
            new EmbeddingGeneratorMetadata(defaultModelDimensions: null));

        Should.Throw<InvalidOperationException>(() => EmbeddingDimensions.Validate(generator));

        AssertNoGeneration(generator);
    }

    [Fact]
    public void Validate_ReportedDimensionMismatch_MessageNamesExpectedDimension()
    {
        var generator = CreateSubstituteWithMetadata(
            new EmbeddingGeneratorMetadata(defaultModelDimensions: 768));

        var exception = Should.Throw<InvalidOperationException>(() => EmbeddingDimensions.Validate(generator));

        exception.Message.ShouldContain(EmbeddingDimensions.Value.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Validate_GeneratorIsNull_ThrowsArgumentNullException()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // intentional null-passing test
        Should.Throw<ArgumentNullException>(() => EmbeddingDimensions.Validate(null!));
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateSubstituteWithMetadata(
        EmbeddingGeneratorMetadata metadata)
    {
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        generator.GetService(typeof(EmbeddingGeneratorMetadata)).Returns(metadata);
        return generator;
    }

    private static void AssertNoGeneration(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        generator.DidNotReceive().GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
    }
}
