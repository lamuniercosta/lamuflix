using LamuFlix.Tests.Common;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(TelemetryCompositionCollection), DisableParallelization = true)]
public sealed class TelemetryCompositionCollection :
    ICollectionFixture<PostgresFixture>,
    ICollectionFixture<RabbitMqFixture>;
