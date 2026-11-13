using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record GetSubtitleLanguagesResult(string? DefaultLanguage, ImmutableArray<SubtitleLanguage> Languages);
