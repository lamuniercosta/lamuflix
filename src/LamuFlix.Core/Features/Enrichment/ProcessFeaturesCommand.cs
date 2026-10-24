using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record ProcessFeaturesCommand(MovieId MovieId, int Attempt);
