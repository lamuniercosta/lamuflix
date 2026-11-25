using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class VersionEndpointCollection
{
    public const string CollectionName = "Version endpoint";
}

[Collection(VersionEndpointCollection.CollectionName)]
public sealed class VersionEndpointTests
{
    private const string Route = "/api/version";
    private const string CommitVariable = "LAMUFLIX_COMMIT";
    private const string ReleaseVariable = "LAMUFLIX_RELEASE";
    private const string FullCommit = "8a2deb9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f";
    private const string ReleaseTag = "v0.1.0";

    [Fact]
    public async Task Version_ConfiguredCommitWithoutRelease_ReturnsFullCommitAndNullRelease()
    {
        // arrange
        var commit = FullCommit;

        // act
        var (status, body) = await GetVersionAsync(commit, null);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe(commit);
        body.GetProperty("release").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Version_ConfiguredCommitAndRelease_ReturnsBothValues()
    {
        // arrange
        var commit = FullCommit;
        var release = ReleaseTag;

        // act
        var (status, body) = await GetVersionAsync(commit, release);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe(commit);
        body.GetProperty("release").GetString().ShouldBe(release);
    }

    [Fact]
    public async Task Version_UnsetCommit_ReturnsUnknown()
    {
        // arrange
        var release = ReleaseTag;

        // act
        var (status, body) = await GetVersionAsync(null, release);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe("unknown");
        body.GetProperty("release").GetString().ShouldBe(release);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Version_BlankCommit_ReturnsUnknown(string commit)
    {
        // arrange
        var release = ReleaseTag;

        // act
        var (status, body) = await GetVersionAsync(commit, release);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe("unknown");
        body.GetProperty("release").GetString().ShouldBe(release);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Version_BlankRelease_ReturnsNull(string release)
    {
        // arrange
        var commit = FullCommit;

        // act
        var (status, body) = await GetVersionAsync(commit, release);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe(commit);
        body.GetProperty("release").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Version_BakedCommitWithNoRuntimeOverride_ReturnsBakedValue()
    {
        // arrange
        var bakedCommit = "2222222222222222222222222222222222222222";

        // act
        var (status, body) = await GetVersionAsync(bakedCommit, null);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe(bakedCommit);
        body.GetProperty("release").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Version_OperatorSuppliedNonblankCommit_ReturnsFullOperatorValue()
    {
        // arrange
        var bakedCommit = FullCommit;
        var operatorCommit = "1111111111111111111111111111111111111111";

        // act
        var (status, body) = await GetVersionAsync(bakedCommit, null, operatorCommit);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe(operatorCommit);
        body.GetProperty("release").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Version_OperatorSuppliedBlankCommit_ReturnsUnknown(string operatorCommit)
    {
        // arrange
        var bakedCommit = FullCommit;

        // act
        var (status, body) = await GetVersionAsync(bakedCommit, null, operatorCommit);

        // assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("commit").GetString().ShouldBe("unknown");
        body.GetProperty("release").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetVersionAsync(
        string? commit,
        string? release,
        string? runtimeCommit = null)
    {
        var previousCommit = Environment.GetEnvironmentVariable(CommitVariable);
        var previousRelease = Environment.GetEnvironmentVariable(ReleaseVariable);
        try
        {
            Environment.SetEnvironmentVariable(CommitVariable, runtimeCommit ?? commit);
            Environment.SetEnvironmentVariable(ReleaseVariable, release);
            await using var factory = new ApiHostFactory();
            using var client = factory.CreateClient();
            var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            return (response.StatusCode, body);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CommitVariable, previousCommit);
            Environment.SetEnvironmentVariable(ReleaseVariable, previousRelease);
        }
    }
}
