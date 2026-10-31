namespace LamuFlix.Core.Ports;

public sealed record ThumbnailResult(bool Success, string? CachePath, string? FailureReason);
