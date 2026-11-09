using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(StreamEndpointCollection), DisableParallelization = true)]
public sealed class StreamEndpointCollection : ICollectionFixture<ApiHostFactory>;
