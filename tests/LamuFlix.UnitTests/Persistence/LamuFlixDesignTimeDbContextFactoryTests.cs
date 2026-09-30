using System;
using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.UnitTests.Persistence;

[CollectionDefinition(nameof(DesignTimeFactoryCollection), DisableParallelization = true)]
public sealed class DesignTimeFactoryCollection;

[Collection(nameof(DesignTimeFactoryCollection))]
public sealed class LamuFlixDesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_MissingVariable_ThrowsWithoutEchoingAValue()
    {
        WithVariable(null, () =>
        {
            var exception = Should.Throw<InvalidOperationException>(
                () => new LamuFlixDesignTimeDbContextFactory().CreateDbContext([]));

            exception.Message.ShouldContain(LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable);
            exception.Message.ShouldNotContain("Host=");
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDbContext_BlankVariable_ThrowsWithoutEchoingTheValue(string value)
    {
        WithVariable(value, () =>
        {
            var exception = Should.Throw<InvalidOperationException>(
                () => new LamuFlixDesignTimeDbContextFactory().CreateDbContext([]));

            exception.Message.ShouldContain(LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable);
            exception.Message.ShouldNotContain("Host=");
        });
    }

    [Fact]
    public void CreateDbContext_SetVariable_ReturnsContext()
    {
        const string connectionString = "Host=unused;Username=unused;Password=unused;Database=unused";
        WithVariable(connectionString, () =>
        {
            using var context = new LamuFlixDesignTimeDbContextFactory().CreateDbContext([]);

            context.ShouldNotBeNull();
        });
    }

    private static void WithVariable(string? value, Action act)
    {
        var previous = Environment.GetEnvironmentVariable(
            LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                value);
            act();
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                previous);
        }
    }
}
