namespace LamuFlix.Infrastructure.Thumbnails;

public sealed record ProcessResult(
    bool Success,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    string? FailureReason);
