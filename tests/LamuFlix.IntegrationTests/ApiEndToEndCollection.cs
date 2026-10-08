using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(ApiEndToEndCollection), DisableParallelization = true)]
public sealed class ApiEndToEndCollection : ICollectionFixture<ApiEndToEndFixture>;
