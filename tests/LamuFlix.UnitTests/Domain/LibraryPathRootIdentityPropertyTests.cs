using System;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class LibraryPathRootIdentityPropertyTests
{
    private const string Root = "C:/library";

    private static readonly string[] RootNamingShapes =
    [
        ".",
        "./",
        "././",
        "C:/library",
        "C:/library/",
        "C:\\library",
        "C:/library/.",
    ];

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool ResolvesToLibraryRoot_EveryRootNamingShape_ReturnsTrue()
    {
        foreach (var shape in RootNamingShapes)
        {
            if (!LibraryPath.ResolvesToLibraryRoot(shape, Root))
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool ResolvesToLibraryRoot_FolderInsideTheRoot_IsNeverTheRoot(NonEmptyString leaf)
    {
        var name = leaf.Get.Trim();
        if (name.Length == 0 || name.Contains("..", StringComparison.Ordinal) || name.Contains('/', StringComparison.Ordinal))
        {
            return true;
        }

        return !LibraryPath.ResolvesToLibraryRoot($"Collections/{name}", Root)
            && !LibraryPath.ResolvesToLibraryRoot($"{Root}/Collections/{name}", Root);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool ResolvesToLibraryRoot_OnlyAgreesWithCanonicalRootIdentity(NonEmptyString raw)
    {
        var value = raw.Get;
        var resolves = LibraryPath.ResolvesToLibraryRoot(value, Root);
        var canonicalises = LibraryPath.TryCanonicalizeUnderRoot(value, Root, out var canonical);

        return resolves == (canonicalises && LibraryPath.IsRootIdentity(canonical));
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool ResolvesToLibraryRoot_AnyShapeRejectsAForeignAbsolutePath(NonEmptyString leaf)
    {
        var escaped = $"D:/elsewhere/{leaf.Get}";

        return !LibraryPath.ResolvesToLibraryRoot(escaped, Root);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool IsRootIdentity_OnlyTheCanonicalDot_ReturnsTrue(NonEmptyString raw)
    {
        var value = raw.Get;

        return LibraryPath.IsRootIdentity(value) == string.Equals(value, ".", StringComparison.Ordinal);
    }
}
