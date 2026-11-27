using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public enum DiscoveryExternalIdStatus
{
    Found,
    ConfirmedAbsence,
    Unresolved,
}

public sealed record TmdbExternalIdLookupResult(DiscoveryExternalIdStatus Status, ImdbId? ImdbId = null)
{
    public static TmdbExternalIdLookupResult Found(ImdbId imdbId) =>
        new(DiscoveryExternalIdStatus.Found, imdbId);

    public static TmdbExternalIdLookupResult ConfirmedAbsence { get; } =
        new(DiscoveryExternalIdStatus.ConfirmedAbsence);

    public static TmdbExternalIdLookupResult Unresolved { get; } =
        new(DiscoveryExternalIdStatus.Unresolved);
}
