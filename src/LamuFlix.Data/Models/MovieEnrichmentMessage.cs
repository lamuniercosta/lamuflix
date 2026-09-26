namespace LamuFlix.Data.Models;

public class MovieEnrichmentMessage
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? ImdbId { get; set; }
    public int RetryCount { get; set; }
}