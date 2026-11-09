using LamuFlix.Tests.Common;
using Xunit;

namespace LamuFlix.IntegrationTests;

// One broker shared by a few short classes. Tests that share a broker run one after another,
// because the topology uses fixed queue names. A class with many broker tests takes its own
// broker through IClassFixture<RabbitMqFixture> instead, so it runs in parallel with the rest.
[CollectionDefinition(nameof(RabbitMqCollection))]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>;