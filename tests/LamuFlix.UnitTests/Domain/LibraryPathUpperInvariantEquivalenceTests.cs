using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FsCheck.Xunit;

namespace LamuFlix.UnitTests.Domain;

/// <summary>
/// Shipping-runtime gate for Patron A8/A11/A12 Option C (FR-021). The persisted key is the whole-string
/// <see cref="string.ToUpperInvariant"/> compared with <see cref="StringComparison.Ordinal"/>, so two identities equal
/// under <see cref="StringComparison.OrdinalIgnoreCase"/> must share one key.
/// </summary>
/// <remarks>
/// The ruled exceptions are the exact verified divergences on the non-invariant shipping runtime: the two Long S
/// pairings (U+017F against U+0073/U+0053) and the 22 Garay case pairings (U+10D50..U+10D65 against
/// U+10D70..U+10D85). The equivalence properties therefore range over strings built from every other code point in
/// the probe alphabet, so no pairing is silently rewritten and nothing outside the exception classes is exempt.
/// The exception classes keep deterministic facts of their own, and one test asserts that no divergence exists
/// outside them.
/// </remarks>
public sealed class LibraryPathUpperInvariantEquivalenceTests
{
    private const int GarayPairCount = 22;
    private const int RuledPairCount = GarayPairCount + 2;
    private const int GarayUpperFirst = 0x10D50;
    private const int GarayUpperLast = 0x10D65;
    private const int GarayLowerFirst = 0x10D70;
    private const int GarayLowerLast = 0x10D85;
    private const int GarayCaseOffset = 0x20;
    private const int LongSUpper = 0x017F;
    private const int LowerS = 0x0073;
    private const int UpperS = 0x0053;

    [Property(MaxTest = 500)]
    [Trait("Category", "Property")]
    public bool EquivalenceHolds_AcrossGeneratedCasePairs(int seed)
    {
        // arrange
        var random = new Random(seed);
        var first = GeneratedText(random);
        var second = Variant(random, first);

        // act / assert
        return Consistent(first, second);
    }

    [Property(MaxTest = 300)]
    [Trait("Category", "Property")]
    public bool EquivalenceHolds_ForEachDocumentedFamily(int index)
    {
        // arrange
        var pair = DocumentedFamilies[(int)((uint)index % (uint)DocumentedFamilies.Count)];

        // act / assert
        return Consistent(pair.First, pair.Second);
    }

    [Property(MaxTest = 1000)]
    [Trait("Category", "Property")]
    public bool EquivalenceHolds_AcrossArbitraryUnicodeCodePoints(int firstSeed, int secondSeed)
    {
        // arrange
        var first = FromSeed(firstSeed);
        var second = FromSeed(secondSeed);

        // act / assert
        return IsRuledPairing(first, second) || Consistent(first, second);
    }

    [Fact]
    public void UpperKey_ForRuledLongSPairings_CollapsesWhileOrdinalIgnoreCaseDiffers()
    {
        // arrange
        const string longS = "\u017F";
        const string lowerS = "s";
        const string upperS = "S";

        // act
        var longSUpper = longS.ToUpperInvariant();

        // assert
        longSUpper.ShouldBe(upperS);
        string.Equals(longSUpper, lowerS.ToUpperInvariant(), StringComparison.Ordinal).ShouldBeTrue();
        string.Equals(longSUpper, upperS.ToUpperInvariant(), StringComparison.Ordinal).ShouldBeTrue();
        longS.Equals(lowerS, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        longS.Equals(upperS, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        lowerS.Equals(upperS, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        Consistent(longS, lowerS).ShouldBeFalse();
        Consistent(longS, upperS).ShouldBeFalse();
    }

    [Fact]
    public void UpperKey_ForRuledGarayPairings_StaysDistinctWhileOrdinalIgnoreCaseMatches()
    {
        // arrange
        var pairings = GarayPairings();

        // act / assert
        pairings.Count.ShouldBe(GarayPairCount);
        foreach (var pairing in pairings)
        {
            var upperKey = pairing.Upper.ToUpperInvariant();
            var lowerKey = pairing.Lower.ToUpperInvariant();

            pairing.Upper.Equals(pairing.Lower, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
            upperKey.ShouldBe(pairing.Upper);
            lowerKey.ShouldBe(pairing.Lower);
            string.Equals(upperKey, lowerKey, StringComparison.Ordinal).ShouldBeFalse();
            Consistent(pairing.Upper, pairing.Lower).ShouldBeFalse();
        }
    }

    [Fact]
    public void DivergentPairings_AcrossProbeAlphabet_AreExactlyTheRuledPairings()
    {
        // arrange
        var observed = new HashSet<(string First, string Second)>();
        var unexpected = new List<string>();

        // act
        for (var first = 0; first < Alphabet.Count; first++)
        {
            for (var second = first + 1; second < Alphabet.Count; second++)
            {
                var candidate = (First: Alphabet[first], Second: Alphabet[second]);
                if (Consistent(candidate.First, candidate.Second))
                {
                    continue;
                }

                observed.Add(candidate);
                if (!IsRuledPairing(candidate.First, candidate.Second))
                {
                    unexpected.Add(candidate.First + " vs " + candidate.Second);
                }
            }
        }

        // assert
        unexpected.ShouldBeEmpty();
        observed.Count.ShouldBe(RuledPairCount);
    }

    private static bool Consistent(string first, string second) =>
        first.Equals(second, StringComparison.OrdinalIgnoreCase)
        == string.Equals(first.ToUpperInvariant(), second.ToUpperInvariant(), StringComparison.Ordinal);

    private static bool IsRuledPairing(string first, string second)
    {
        var firstCodePoint = ToCodePoint(first);
        var secondCodePoint = ToCodePoint(second);
        if (firstCodePoint == LongSUpper)
        {
            return secondCodePoint is LowerS or UpperS;
        }

        if (secondCodePoint == LongSUpper)
        {
            return firstCodePoint is LowerS or UpperS;
        }

        return IsGarayPairing(firstCodePoint, secondCodePoint)
            || IsGarayPairing(secondCodePoint, firstCodePoint);
    }

    private static bool IsGarayPairing(int upperCodePoint, int lowerCodePoint) =>
        upperCodePoint is >= GarayUpperFirst and <= GarayUpperLast
        && lowerCodePoint == upperCodePoint + GarayCaseOffset;

    private static bool IsRuledCodePoint(string element)
    {
        var codePoint = ToCodePoint(element);
        return codePoint == LongSUpper
            || codePoint is >= GarayUpperFirst and <= GarayUpperLast
            || codePoint is >= GarayLowerFirst and <= GarayLowerLast;
    }

    private static int ToCodePoint(string element) =>
        string.IsNullOrEmpty(element) ? -1 : char.ConvertToUtf32(element, 0);

    private static string FromCodePoint(int codePoint) => char.ConvertFromUtf32(codePoint);

    private static string FromSeed(int seed)
    {
        var magnitude = (uint)seed % 0x110000u;
        var codePoint = (int)(0xD800u <= magnitude && magnitude <= 0xDFFFu ? magnitude - 0xD800u : magnitude);
        return FromCodePoint(codePoint);
    }

    private static IEnumerable<string> Elements(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var length = char.IsHighSurrogate(value[index]) ? 2 : 1;
            yield return value.Substring(index, length);
            index += length - 1;
        }
    }

    private static string GeneratedText(Random random)
    {
        var length = random.Next(1, 5);
        var builder = new StringBuilder(length);
        for (var index = 0; index < length; index++)
        {
            builder.Append(EquivalenceDomain[random.Next(EquivalenceDomain.Count)]);
        }

        return builder.ToString();
    }

    private static string Variant(Random random, string original)
    {
        var variant = new StringBuilder(original.Length);
        foreach (var element in Elements(original))
        {
            variant.Append(random.Next(3) switch
            {
                0 => element,
                1 => element.ToUpperInvariant(),
                _ => element.ToLowerInvariant(),
            });
        }

        return variant.ToString();
    }

    private static List<(string Upper, string Lower)> GarayPairings()
    {
        var pairings = new List<(string Upper, string Lower)>(GarayPairCount);
        for (var offset = 0; offset < GarayPairCount; offset++)
        {
            pairings.Add((FromCodePoint(GarayUpperFirst + offset), FromCodePoint(GarayLowerFirst + offset)));
        }

        return pairings;
    }

    private static List<(string First, string Second)> BuildRuledPairings()
    {
        var pairings = new List<(string First, string Second)>(RuledPairCount)
        {
            ("\u017F", "s"),
            ("\u017F", "S"),
        };
        pairings.AddRange(GarayPairings());
        return pairings;
    }

    private static List<string> BuildAlphabet()
    {
        var elements = new List<string>(BaseAlphabet);
        foreach (var offset in Enumerable.Range(0, GarayPairCount))
        {
            elements.Add(FromCodePoint(GarayUpperFirst + offset));
            elements.Add(FromCodePoint(GarayLowerFirst + offset));
        }

        return elements;
    }

    private static readonly List<(string First, string Second)> RuledPairings = BuildRuledPairings();

    private static readonly List<(string First, string Second)> DocumentedFamilies =
    [
        ("\U00010400", "\U00010428"),
        ("\u00B5", "\u039C"),
        ("\u00B5", "\u03BC"),
        ("\u03C2", "\u03C3"),
        ("\u0345", "\u0399"),
        ("\u01C4", "\u01C6"),
        ("\u01F1", "\u01F3"),
        ("\u00DF", "\u1E9E"),
        ("\u00DF", "SS"),
        ("\uFB00", "FF"),
        ("\uFB01", "FI"),
        ("\uFB03", "FFI"),
        ("i", "\u0130"),
        ("I", "\u0131"),
        ("e\u0301", "\u00E9"),
        ("\u212A", "K"),
        ("Heat (1995)", "heat (1995)"),
    ];

    private static readonly List<string> BaseAlphabet =
    [
        "a",
        "A",
        "k",
        "K",
        "s",
        "S",
        "\u017F",
        "\u00B5",
        "\u039C",
        "\u03BC",
        "\u03C2",
        "\u03C3",
        "\u0345",
        "\u0399",
        "\u01C4",
        "\u01C6",
        "\u01F1",
        "\u01F3",
        "\u00DF",
        "\u1E9E",
        "\uFB00",
        "\uFB01",
        "\uFB03",
        "i",
        "I",
        "\u0130",
        "\u0131",
        "e\u0301",
        "\u00E9",
        "\u212A",
        "\U00010400",
        "\U00010428",
    ];

    private static readonly List<string> Alphabet = BuildAlphabet();

    private static readonly List<string> EquivalenceDomain =
        Alphabet.FindAll(element => !IsRuledCodePoint(element));

    static LibraryPathUpperInvariantEquivalenceTests()
    {
        foreach (var pairing in RuledPairings)
        {
            if (!Alphabet.Contains(pairing.First) || !Alphabet.Contains(pairing.Second))
            {
                throw new InvalidOperationException("Ruled pairing is missing from the probe alphabet.");
            }
        }

        if (EquivalenceDomain.Exists(IsRuledCodePoint))
        {
            throw new InvalidOperationException("Equivalence domain must exclude the ruled code points.");
        }
    }
}
