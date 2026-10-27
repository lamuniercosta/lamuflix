using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiCorsTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string DevServerOrigin = "http://localhost:5173";
    private const string OtherOrigin = "http://localhost:3000";
    private const string LivenessRoute = "/health/live";
    private const string NegotiateRoute = "/hubs/enrichment/negotiate?negotiateVersion=1";
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string ExposeHeadersHeader = "Access-Control-Expose-Headers";
    private const string AllowMethodsHeader = "Access-Control-Allow-Methods";
    private const string AllowHeadersHeader = "Access-Control-Allow-Headers";
    private const string AllowCredentialsHeader = "Access-Control-Allow-Credentials";

    [Fact]
    public async Task LivenessRequest_DevServerOrigin_CarriesAllowOriginAndExposesLocation()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, LivenessRoute);
        request.Headers.Add("Origin", DevServerOrigin);

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.Headers.GetValues(AllowOriginHeader).ShouldHaveSingleItem().ShouldBe(DevServerOrigin);
        response.Headers.GetValues(ExposeHeadersHeader).ShouldHaveSingleItem().ShouldContain("Location");
    }

    [Fact]
    public async Task Preflight_DevServerOrigin_CarriesAllowOriginMethodAndHeaders()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, LivenessRoute);
        request.Headers.Add("Origin", DevServerOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.Headers.GetValues(AllowOriginHeader).ShouldHaveSingleItem().ShouldBe(DevServerOrigin);
        response.Headers.GetValues(AllowMethodsHeader).ShouldHaveSingleItem().ShouldBe("GET");
        response.Headers.GetValues(AllowHeadersHeader).ShouldHaveSingleItem().ShouldBe("content-type");
    }

    [Fact]
    public async Task LivenessRequest_OtherOrigin_CarriesNoAllowOriginHeader()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, LivenessRoute);
        request.Headers.Add("Origin", OtherOrigin);

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.Headers.Contains(AllowOriginHeader).ShouldBeFalse();
    }

    [Fact]
    public async Task Preflight_HubNegotiate_DevServerOrigin_CarriesAllowOriginAnyMethodAndHeadersWithoutCredentials()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, NegotiateRoute);
        request.Headers.Add("Origin", DevServerOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-signalr-user-agent");

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.Headers.GetValues(AllowOriginHeader).ShouldHaveSingleItem().ShouldBe(DevServerOrigin);
        response.Headers.GetValues(AllowMethodsHeader).ShouldHaveSingleItem().ShouldBe("POST");
        response.Headers.GetValues(AllowHeadersHeader).ShouldHaveSingleItem().ShouldBe("content-type,x-signalr-user-agent");
        response.Headers.Contains(AllowCredentialsHeader).ShouldBeFalse();
    }

    [Fact]
    public async Task NegotiateRequest_DevServerOrigin_CarriesAllowOriginAndExposesLocation()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, NegotiateRoute);
        request.Headers.Add("Origin", DevServerOrigin);

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Headers.GetValues(AllowOriginHeader).ShouldHaveSingleItem().ShouldBe(DevServerOrigin);
        response.Headers.GetValues(ExposeHeadersHeader).ShouldHaveSingleItem().ShouldContain("Location");
        response.Headers.Contains(AllowCredentialsHeader).ShouldBeFalse();
    }

    [Fact]
    public async Task NegotiateRequest_OtherOrigin_CarriesNoAllowOriginHeader()
    {
        // arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, NegotiateRoute);
        request.Headers.Add("Origin", OtherOrigin);

        // act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        response.Headers.Contains(AllowOriginHeader).ShouldBeFalse();
    }
}