using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public static class MovieQueryFixture
{
    public static Arbitrary<MovieQuery> Queries() => Arb.From(Generator());

    private static Gen<MovieQuery> Generator() =>
        from text in TextGen()
        from genres in IdList()
        from actors in IdList()
        from runtime in RuntimeGen()
        from year in YearGen()
        from statuses in StatusGen()
        from watchlist in Gen.Elements<bool?>(null, true, false)
        from sort in Gen.Elements(MovieSort.List.ToArray())
        from direction in Gen.Elements(SortDirection.List.ToArray())
        from number in Gen.Choose(1, 8)
        from size in Gen.Choose(1, 100)
        select new MovieQuery
        {
            Text = text,
            GenreIds = genres,
            ActorIds = actors,
            Runtime = runtime,
            Year = year,
            Statuses = statuses,
            InWatchlist = watchlist,
            Sort = sort,
            Direction = direction,
            Page = new Page(number, size),
        };

    private static Gen<string?> TextGen() =>
        Gen.Elements(null, string.Empty, "matrix", "a&b", "x=y", "q \"z", "a b", "ü&=");

    private static Gen<ImmutableArray<int>> IdList() =>
        Gen.Choose(0, 4).SelectMany(count =>
            Gen.ListOf(Gen.Choose(1, 30), count).Select(values => values.ToImmutableArray()));

    private static Gen<ImmutableArray<EnrichmentStatus>> StatusGen() =>
        Gen.Choose(0, EnrichmentStatus.List.Count).SelectMany(count =>
            Gen.Shuffle(EnrichmentStatus.List.ToArray())
                .Select(values => values.Take(count).ToImmutableArray()));

    private static Gen<RuntimeRange?> RuntimeGen()
    {
        var absentOrEmpty = Gen.Elements(
            null,
            new RuntimeRange(null, null, false),
            new RuntimeRange(null, null, true));
        var bounded =
            from min in Gen.Choose(0, 120)
            from span in Gen.Choose(0, 60)
            from include in Gen.Elements(true, false)
            select new RuntimeRange(min, min + span, include);
        return Gen.OneOf(absentOrEmpty, bounded);
    }

    private static Gen<YearRange?> YearGen()
    {
        var absentOrEmpty = Gen.Elements(null, new YearRange(null, null));
        var bounded =
            from min in Gen.Choose(1900, 2020)
            from span in Gen.Choose(0, 10)
            select new YearRange(min, min + span);
        return Gen.OneOf(absentOrEmpty, bounded);
    }
}
