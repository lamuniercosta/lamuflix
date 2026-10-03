namespace LamuFlix.Api.Endpoints;

// ReSharper disable UnusedAutoPropertyAccessor.Global // [AsParameters] binding sets these properties reflectively.
internal sealed record BrowseMoviesRequest
{
    public string? Text { get; set; }

    public string[]? GenreIds { get; set; }

    public string[]? ActorIds { get; set; }

    public string? RuntimeMin { get; set; }

    public string? RuntimeMax { get; set; }

    public string? RuntimeIncludeUnknown { get; set; }

    public string? YearMin { get; set; }

    public string? YearMax { get; set; }

    public string[]? Statuses { get; set; }

    public string? InWatchlist { get; set; }

    public string? Sort { get; set; }

    public string? Direction { get; set; }

    public string? Page { get; set; }

    public string? PageSize { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}
