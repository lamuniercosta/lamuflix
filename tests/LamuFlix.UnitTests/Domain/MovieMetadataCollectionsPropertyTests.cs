using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataCollectionsPropertyTests
{
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool GenresAndWriters_SameContentDifferentInstances_AreEqualWithEqualHashAndOrder(
        string[] genres,
        string[] writers)
    {
        var suppliedGenres = genres ?? [];
        var suppliedWriters = writers ?? [];

        var left = new MovieMetadata("Title", genres: suppliedGenres, writers: suppliedWriters);
        var right = new MovieMetadata("Title", genres: suppliedGenres, writers: suppliedWriters);

        return HaveSameEqualityAndHash(left, right)
            && MatchSuppliedSnapshot(left, suppliedGenres, suppliedWriters);
    }

    private static bool HaveSameEqualityAndHash(MovieMetadata left, MovieMetadata right) =>
        left.Equals(right)
        && right.Equals(left)
        && left.GetHashCode() == right.GetHashCode();

    private static bool MatchSuppliedSnapshot(MovieMetadata metadata, string[] suppliedGenres, string[] suppliedWriters) =>
        metadata.Genres.SequenceEqual(suppliedGenres)
        && metadata.Writers.SequenceEqual(suppliedWriters)
        && metadata.Genres.Length == suppliedGenres.Length
        && metadata.Writers.Length == suppliedWriters.Length;

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Constructor_CopiesSuppliedSequences_SoSourceMutationDoesNotAffectStoredCollections(
        string[] genres,
        string[] writers)
    {
        var suppliedGenres = genres ?? [];
        var suppliedWriters = writers ?? [];

        var metadata = new MovieMetadata("Title", genres: suppliedGenres, writers: suppliedWriters);
        var expectedGenres = suppliedGenres.ToArray();
        var expectedWriters = suppliedWriters.ToArray();

        if (suppliedGenres.Length > 0)
        {
            suppliedGenres[0] = "MUTATED";
        }

        if (suppliedWriters.Length > 0)
        {
            suppliedWriters[0] = "MUTATED";
        }

        return metadata.Genres.SequenceEqual(expectedGenres)
            && metadata.Writers.SequenceEqual(expectedWriters);
    }
}
