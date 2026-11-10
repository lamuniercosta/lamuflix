using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(SubtitleEndpointCollection), DisableParallelization = true)]
public sealed class SubtitleEndpointCollection : ICollectionFixture<ApiHostFactory>;
