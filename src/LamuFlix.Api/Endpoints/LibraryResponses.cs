using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global
// Serialized by System.Text.Json as the frozen wire shapes, never read in code
internal sealed record BrowseMoviesResponse(IReadOnlyList<MovieSummaryResponse> Items, int TotalCount)
{
    public static BrowseMoviesResponse From(PagedResult<MovieSummary> page) =>
        new(
            [.. page.Items.Select(summary => new MovieSummaryResponse(summary.Id.Value, summary.Title))],
            page.TotalCount);
}

internal sealed record MovieSummaryResponse(int Id, string Title);

internal sealed record MovieDetailsResponse(int Id, string Title, string Path, string Format, MovieMetadataResponse? Metadata)
{
    public static MovieDetailsResponse From(MovieDetails details) =>
        new(
            details.Id.Value,
            details.Title,
            details.Path.Value,
            details.Format.Extension,
            details.Metadata is null ? null : MovieMetadataResponse.From(details.Metadata));
}

internal sealed record MovieMetadataResponse(
    string Title,
    string? Synopsis,
    int? ReleaseYear,
    int? Runtime,
    decimal? ImdbRating,
    string? ImdbId)
{
    public static MovieMetadataResponse From(MovieMetadata metadata) =>
        new(
            metadata.Title,
            metadata.Synopsis,
            metadata.ReleaseYear?.Value,
            metadata.Runtime?.Minutes,
            metadata.ImdbRating?.Value,
            metadata.ImdbId?.Value);
}
// ReSharper restore NotAccessedPositionalProperty.Global