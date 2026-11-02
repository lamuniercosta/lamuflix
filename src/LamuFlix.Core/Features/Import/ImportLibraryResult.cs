namespace LamuFlix.Core.Features.Import;

/// <summary>
/// Count of Candidate Movie Folders whose movie and staged enrichment were saved
/// before the invocation drained. Says nothing about enrichment completion.
/// </summary>
public sealed record ImportLibraryResult(long ImportedCount);
