using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.FileSystem;

/// <summary>The title and optional release year recovered from a library folder name.</summary>
public sealed record CleanedFolderName(string Title, ReleaseYear? Year);
