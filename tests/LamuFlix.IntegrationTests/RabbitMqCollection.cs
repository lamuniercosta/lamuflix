using LamuFlix.Tests.Common;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(RabbitMqCollection))]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>;