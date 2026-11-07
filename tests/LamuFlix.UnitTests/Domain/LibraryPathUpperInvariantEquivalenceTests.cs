using System;
using System.Collections.Generic;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

/// <summary>
/// Shipping-runtime gate for Patron A8 Option C (FR-021): the derived key is the whole-string
/// <see cref="string.ToUpperInvariant"/>, so two identities equal under OrdinalIgnoreCase must
/// share one key. A counterexample blocks acceptance and is never waived.
/// </summary>
public sealed class LibraryPathUpperInvariantEquivalenceTests
{
    [Property(MaxTest = 500)]
    [Trait("Category", "Property")]
    public bool EquivalenceHolds_AcrossGeneratedCasePairs(int seed)
    {
        // arrange
        var random = new Random(seed);
        var first = GeneratedText(random);
        var second = CaseVariant(random, first);

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

    [Fact]
    public void LongSPair_OrdinalIgnoreCaseDiffersFromUpperInvariant_BlocksTheGate()
    {
        // arrange
        const string longS = "\u017F";
        const string plainS = "s";

        // act
        var ordinalIgnoreCase = longS.Equals(plainS, StringComparison.OrdinalIgnoreCase);
        var upperEqual = string.Equals(
            longS.ToUpperInvariant(),
            plainS.ToUpperInvariant(),
            StringComparison.Ordinal);

        // assert
        ordinalIgnoreCase.ShouldBe(upperEqual);
    }

    private static bool Consistent(string first, string second) =>
        first.Equals(second, StringComparison.OrdinalIgnoreCase)
        == string.Equals(first.ToUpperInvariant(), second.ToUpperInvariant(), StringComparison.Ordinal);

    private static string GeneratedText(Random random)
    {
        var length = random.Next(1, 4);
        var builder = new System.Text.StringBuilder(length);
        for (var index = 0; index < length; index++)
        {
            builder.Append(CasePairs[random.Next(CasePairs.Count)].Lower);
        }

        return builder.ToString();
    }

    private static string CaseVariant(Random random, string original)
    {
        var variant = new System.Text.StringBuilder(original.Length);
        foreach (var character in original)
        {
            var index = CasePairs.FindIndex(pair => pair.Lower[0] == character);
            if (index < 0)
            {
                variant.Append(character);
                continue;
            }

            var options = CasePairs[index];
            variant.Append(random.Next(2) == 0 ? options.Lower : options.Upper);
        }

        return variant.ToString();
    }

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
        ("\u017F", "s"),
        ("Heat (1995)", "heat (1995)"),
    ];

    private static readonly List<(string Lower, string Upper)> CasePairs =
    [
        ("a", "A"),
        ("k", "K"),
        ("\u212A", "k"),
        ("\u00B5", "\u039C"),
        ("\u03BC", "\u039C"),
        ("\u03C2", "\u03C3"),
        ("\u01C6", "\u01C4"),
        ("\U00010428", "\U00010400"),
        ("i", "I"),
        ("\u0131", "I"),
        ("\u0130", "i"),
        ("\u017F", "s"),
    ];
}
