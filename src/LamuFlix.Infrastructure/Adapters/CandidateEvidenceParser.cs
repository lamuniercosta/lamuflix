using System;
using System.Globalization;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Infrastructure.Adapters;

public sealed record CandidateEvidencePayload(
    bool ProviderConfirmedAbsent,
    string? ImdbId,
    string? Rating,
    string? Votes,
    int? CollectionId = null);

internal static class CandidateEvidenceParser
{
    private static readonly TimeSpan CompleteFreshness = TimeSpan.FromDays(30);

    private static readonly TimeSpan ConfirmedAbsenceFreshness = TimeSpan.FromDays(7);

    internal static CandidateImdbEvidence Parse(CandidateEvidencePayload payload, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var imdbId = ParseImdbId(payload.ImdbId);
        var rating = ParseRating(payload.Rating);
        var votes = ParseVotes(payload.Votes);
        var collectionId = payload.CollectionId is > 0 ? payload.CollectionId : null;

        if (HasKnownFailure(rating, votes))
        {
            return new CandidateImdbEvidence(
                imdbId,
                collectionId,
                rating,
                votes,
                CandidateEvidenceOutcome.Complete,
                now + CompleteFreshness);
        }

        if (payload.ProviderConfirmedAbsent)
        {
            return new CandidateImdbEvidence(
                imdbId,
                collectionId,
                rating,
                votes,
                CandidateEvidenceOutcome.ConfirmedAbsence,
                now + ConfirmedAbsenceFreshness);
        }

        return rating is >= CandidateImdbEvidence.MinimumRating
            && votes is >= CandidateImdbEvidence.MinimumVotes
            ? new CandidateImdbEvidence(
                imdbId,
                collectionId,
                rating,
                votes,
                CandidateEvidenceOutcome.Complete,
                now + CompleteFreshness)
            : new CandidateImdbEvidence(
                imdbId,
                collectionId,
                rating,
                votes,
                CandidateEvidenceOutcome.Unresolved);
    }

    private static bool HasKnownFailure(decimal? rating, int? votes) =>
        rating is < CandidateImdbEvidence.MinimumRating || votes is < CandidateImdbEvidence.MinimumVotes;

    private static string? ParseImdbId(string? value) =>
        Core.Domain.ImdbId.TryCreate(value, out var parsed) ? parsed.Value : null;

    private static decimal? ParseRating(string? value)
    {
        if (Trim(value) is not { } text || text.Length == 0)
        {
            return null;
        }

        return decimal.TryParse(
                text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var rating)
            && rating is >= 0 and <= 10
                ? rating
                : null;
    }

    private static int? ParseVotes(string? value) =>
        Trim(value) is { } text
        && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var votes)
            ? votes
            : null;

    private static string? Trim(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return string.Equals(trimmed, "N/A", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
    }
}
