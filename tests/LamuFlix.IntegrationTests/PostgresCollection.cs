using LamuFlix.Tests.Common;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;