using System;

namespace LamuFlix.Core.Domain;

public sealed record PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt);
