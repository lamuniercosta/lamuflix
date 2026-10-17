using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.AI;

namespace LamuFlix.UnitTests;

public sealed class FakeEmbeddingGeneratorTests
{
    private static readonly string[] RepresentativeTexts =
    [
        "a",
        "Hello, world",
        "MiXeD",
        "mixed",
        "two  spaces",
        "two spaces",
        string.Empty,
    ];

    public static TheoryData<int?> Non384ReportedDimensions => new()
    {
        0,
        1,
        768,
        (int?)null,
    };

    [Fact]
    public async Task GenerateAsync_SameText_YieldsSameVectorOnOneInstanceAndAnother()
    {
        var generator = new FakeEmbeddingGenerator();
        var otherGenerator = new FakeEmbeddingGenerator();

        var first = await GenerateVectorAsync(generator, "stable text");
        var second = await GenerateVectorAsync(generator, "stable text");
        var fromOtherInstance = await GenerateVectorAsync(otherGenerator, "stable text");

        first.Length.ShouldBe(EmbeddingDimensions.Value);
        first.SequenceEqual(second).ShouldBeTrue();
        first.SequenceEqual(fromOtherInstance).ShouldBeTrue();
    }

    [Fact]
    public async Task GenerateAsync_RepresentativeDistinctTexts_YieldDifferentVectors()
    {
        var generator = new FakeEmbeddingGenerator();
        var vectors = new List<float[]>();

        foreach (var text in RepresentativeTexts)
        {
            vectors.Add(await GenerateVectorAsync(generator, text));
        }

        for (var left = 0; left < vectors.Count; left++)
        {
            for (var right = left + 1; right < vectors.Count; right++)
            {
                vectors[left].SequenceEqual(vectors[right]).ShouldBeFalse();
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_EachVector_IsFiniteNonZeroAndUnitLength()
    {
        var generator = new FakeEmbeddingGenerator();

        foreach (var text in RepresentativeTexts)
        {
            var vector = await GenerateVectorAsync(generator, text);

            vector.Length.ShouldBe(EmbeddingDimensions.Value);
            vector.Any(value => !float.IsFinite(value)).ShouldBeFalse();
            vector.Any(value => value != 0f).ShouldBeTrue();
            Math.Abs(ComputeNorm(vector) - 1.0).ShouldBeLessThanOrEqualTo(1e-6);
        }
    }

    [Fact]
    public async Task GenerateAsync_DocumentedInput_MatchesFixedCoordinates()
    {
        var vector = await GenerateVectorAsync(new FakeEmbeddingGenerator(), "a");

        vector[0].ShouldBe(0.00911402702f, 0.0);
        vector[1].ShouldBe(-0.00423177239f, 0.0);
        vector[7].ShouldBe(0.0727573931f, 0.0);
        vector[8].ShouldBe(0.0636756271f, 0.0);
        vector[15].ShouldBe(-0.0686407834f, 0.0);
        vector[16].ShouldBe(-0.0733914003f, 0.0);
        vector[383].ShouldBe(0.0482403785f, 0.0);
    }

    [Fact]
    public async Task GenerateAsync_BatchWithDuplicates_PreservesOrderAndUsesFreshBuffers()
    {
        var generator = new FakeEmbeddingGenerator();
        var inputs = new[] { "alpha", "beta", "alpha" };

        var embeddings = await GenerateAsync(generator, inputs);

        embeddings.Count.ShouldBe(3);
        var alpha = await GenerateVectorAsync(generator, "alpha");
        var beta = await GenerateVectorAsync(generator, "beta");
        embeddings[0].Vector.ToArray().SequenceEqual(alpha).ShouldBeTrue();
        embeddings[1].Vector.ToArray().SequenceEqual(beta).ShouldBeTrue();
        embeddings[2].Vector.ToArray().SequenceEqual(alpha).ShouldBeTrue();

        var buffers = embeddings.Select(GetBuffer).ToArray();
        ReferenceEquals(buffers[0], buffers[1]).ShouldBeFalse();
        ReferenceEquals(buffers[0], buffers[2]).ShouldBeFalse();

        var secondCall = await GenerateAsync(generator, new[] { "alpha" });
        ReferenceEquals(buffers[0], GetBuffer(secondCall[0])).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_EmptyBatch_ReturnsNoVectors()
    {
        var embeddings = await GenerateAsync(new FakeEmbeddingGenerator(), Array.Empty<string>());

        embeddings.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_EmptyText_ReturnsOneVector()
    {
        var vector = await GenerateVectorAsync(new FakeEmbeddingGenerator(), string.Empty);

        vector.Length.ShouldBe(EmbeddingDimensions.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateAsync_MissingArgument_ThrowsArgumentNullException(bool nullBatch)
    {
        var generator = new FakeEmbeddingGenerator();
        // ReSharper disable once NullableWarningSuppressionIsUsed // intentional null-element test input
        string[]? values = nullBatch ? null : new[] { "ok", null! };

        // ReSharper disable once NullableWarningSuppressionIsUsed // intentional null-passing test
        await Should.ThrowAsync<ArgumentNullException>(() => GenerateAsync(generator, values!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GenerateAsync_AcceptedOptions_YieldsOneVector(int shape)
    {
        var options = shape switch
        {
            0 => null,
            1 => new EmbeddingGenerationOptions(),
            _ => new EmbeddingGenerationOptions { Dimensions = EmbeddingDimensions.Value },
        };

        var embeddings = await GenerateAsync(
            new FakeEmbeddingGenerator(),
            new[] { "text" },
            options);

        embeddings.Count.ShouldBe(1);
        embeddings[0].Vector.Length.ShouldBe(EmbeddingDimensions.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(383)]
    [InlineData(385)]
    [InlineData(768)]
    public async Task GenerateAsync_RequestedDimensionOtherThan384_ThrowsArgumentOutOfRangeException(
        int dimensions)
    {
        var options = new EmbeddingGenerationOptions { Dimensions = dimensions };

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => GenerateAsync(new FakeEmbeddingGenerator(), new[] { "text" }, options));
    }

    [Fact]
    public async Task GenerateAsync_TokenCancelledBeforeCall_ThrowsWithoutEnumerating()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var probe = new ProbeEnumerable(new[] { "one" });

        await AssertCancellationAsync(
            () => new FakeEmbeddingGenerator().GenerateAsync(probe, cancellationToken: cancellation.Token));

        probe.EnumeratorCalls.ShouldBe(0);
        probe.MoveNextCalls.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_TokenCancelledBetweenItems_ThrowsWithoutPullingAnotherItem()
    {
        using var cancellation = new CancellationTokenSource();
        var probe = new ProbeEnumerable(
            new[] { "one", "two", "three" },
            // ReSharper disable once AccessToDisposedClosure // callback runs before disposal within the test
            call => CancelOnCall(cancellation, call, 1));

        await AssertCancellationAsync(
            () => new FakeEmbeddingGenerator().GenerateAsync(probe, cancellationToken: cancellation.Token));

        probe.MoveNextCalls.ShouldBe(2);
        probe.ItemsYielded.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateAsync_TokenCancelledInFinalMoveNext_ThrowsInsteadOfCompleting()
    {
        using var cancellation = new CancellationTokenSource();
        var probe = new ProbeEnumerable(
            new[] { "only" },
            // ReSharper disable once AccessToDisposedClosure // callback runs before disposal within the test
            call => CancelOnCall(cancellation, call, 1));

        await AssertCancellationAsync(
            () => new FakeEmbeddingGenerator().GenerateAsync(probe, cancellationToken: cancellation.Token));

        probe.ItemsYielded.ShouldBe(1);
        probe.MoveNextCalls.ShouldBe(2);
    }

    [Theory]
    [MemberData(nameof(Non384ReportedDimensions))]
    public async Task GenerateAsync_ReportedDimensionOverride_StillYields384AndValidationRejects(
        int? reportedDimensions)
    {
        var generator = new FakeEmbeddingGenerator(reportedDimensions);

        var metadata = generator.GetService(typeof(EmbeddingGeneratorMetadata))
            .ShouldBeOfType<EmbeddingGeneratorMetadata>();
        metadata.DefaultModelDimensions.ShouldBe(reportedDimensions);
        metadata.ProviderName.ShouldBeNull();
        metadata.ProviderUri.ShouldBeNull();
        metadata.DefaultModelId.ShouldBeNull();

        var vector = await GenerateVectorAsync(generator, "override");

        vector.Length.ShouldBe(EmbeddingDimensions.Value);
        Should.Throw<InvalidOperationException>(() => EmbeddingDimensions.Validate(generator));
    }

    [Fact]
    public void GetService_NullServiceType_ThrowsArgumentNullException()
    {
        var generator = new FakeEmbeddingGenerator();

        // ReSharper disable once NullableWarningSuppressionIsUsed // intentional null-passing test
        Should.Throw<ArgumentNullException>(() => generator.GetService(null!));
    }

    [Fact]
    public void GetService_NonNullServiceKey_ReturnsNull()
    {
        var generator = new FakeEmbeddingGenerator();

        generator.GetService(typeof(EmbeddingGeneratorMetadata), "key").ShouldBeNull();
        generator.GetService(typeof(FakeEmbeddingGenerator), "key").ShouldBeNull();
    }

    [Fact]
    public void GetService_MetadataType_ReturnsMetadata()
    {
        var generator = new FakeEmbeddingGenerator();

        var metadata = generator.GetService(typeof(EmbeddingGeneratorMetadata))
            .ShouldBeOfType<EmbeddingGeneratorMetadata>();

        metadata.DefaultModelDimensions.ShouldBe(EmbeddingDimensions.Value);
    }

    [Fact]
    public void GetService_GeneratorType_ReturnsSelf()
    {
        var generator = new FakeEmbeddingGenerator();

        generator.GetService(typeof(FakeEmbeddingGenerator)).ShouldBeSameAs(generator);
        generator.GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>))
            .ShouldBeSameAs(generator);
    }

    [Fact]
    public void GetService_UnrelatedType_ReturnsNull()
    {
        var generator = new FakeEmbeddingGenerator();

        generator.GetService(typeof(string)).ShouldBeNull();
    }

    [Fact]
    public async Task Dispose_DoesNothing()
    {
        var generator = new FakeEmbeddingGenerator();

        Should.NotThrow(() => generator.Dispose());

        var vector = await GenerateVectorAsync(generator, "after dispose");
        vector.Length.ShouldBe(EmbeddingDimensions.Value);
    }

    private static Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        FakeEmbeddingGenerator generator,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null)
    {
        return generator.GenerateAsync(values, options, TestContext.Current.CancellationToken);
    }

    private static async Task<float[]> GenerateVectorAsync(
        FakeEmbeddingGenerator generator,
        string text)
    {
        var embeddings = await GenerateAsync(generator, new[] { text });
        embeddings.Count.ShouldBe(1);
        return embeddings[0].Vector.ToArray();
    }

    private static async Task AssertCancellationAsync(
        Func<Task<GeneratedEmbeddings<Embedding<float>>>> action)
    {
        GeneratedEmbeddings<Embedding<float>>? result = null;

        await Should.ThrowAsync<OperationCanceledException>(async () => result = await action());

        result.ShouldBeNull();
    }

    private static void CancelOnCall(
        CancellationTokenSource cancellation,
        int call,
        int cancelledCall)
    {
        if (call == cancelledCall)
        {
            cancellation.Cancel();
        }
    }

    private static float[] GetBuffer(Embedding<float> embedding)
    {
        MemoryMarshal.TryGetArray(embedding.Vector, out ArraySegment<float> segment).ShouldBeTrue();
        segment.Array.ShouldNotBeNull();
        // ReSharper disable once NullableWarningSuppressionIsUsed // asserted non-null on the previous line
        return segment.Array!;
    }

    private static double ComputeNorm(float[] vector)
    {
        var sumOfSquares = 0.0;
        foreach (var value in vector)
        {
            sumOfSquares += (double)value * value;
        }

        return Math.Sqrt(sumOfSquares);
    }

    private sealed class ProbeEnumerable : IEnumerable<string>
    {
        private readonly string[] items;
        private readonly Action<int>? onMoveNext;

        public ProbeEnumerable(string[] items, Action<int>? onMoveNext = null)
        {
            this.items = items;
            this.onMoveNext = onMoveNext;
        }

        public int EnumeratorCalls { get; private set; }

        public int MoveNextCalls { get; private set; }

        public int ItemsYielded { get; private set; }

        public IEnumerator<string> GetEnumerator()
        {
            EnumeratorCalls++;
            return new ProbeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class ProbeEnumerator : IEnumerator<string>
        {
            private readonly ProbeEnumerable owner;
            private int index = -1;

            public ProbeEnumerator(ProbeEnumerable owner)
            {
                this.owner = owner;
            }

            public string Current => owner.items[index];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var call = owner.MoveNextCalls++;
                owner.onMoveNext?.Invoke(call);

                if (index + 1 >= owner.items.Length)
                {
                    return false;
                }

                index++;
                owner.ItemsYielded++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
