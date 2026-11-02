namespace LamuFlix.Core.Features.Import;

/// <summary>
/// One-shot recursive library import. Payload-free: the root and the concurrency
/// limit come from the startup-validated <c>LibraryOptions</c>.
/// </summary>
public sealed record ImportLibraryCommand;
