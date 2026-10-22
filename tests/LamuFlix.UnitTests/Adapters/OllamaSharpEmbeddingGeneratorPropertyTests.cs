using System;
using FsCheck.Xunit;
using LamuFlix.Infrastructure.Adapters;

namespace LamuFlix.UnitTests.Adapters;

public sealed class OllamaSharpEmbeddingGeneratorPropertyTests
{
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool SafeEndpoint_KeepsOnlySchemeHostAndPort(bool useHttps, uint hostSeed, int portSeed)
    {
        var (endpoint, host, port) = AdversarialEndpoint(useHttps, hostSeed, portSeed);

        var safe = OllamaSharpEmbeddingGenerator.SafeEndpoint(endpoint);

        return safe == $"{Scheme(useHttps)}://{host}:{port}";
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool SafeEndpoint_NeverLeaksCredentialsPathOrQuery(bool useHttps, uint hostSeed, int portSeed)
    {
        var (endpoint, host, port) = AdversarialEndpoint(useHttps, hostSeed, portSeed);

        var safe = OllamaSharpEmbeddingGenerator.SafeEndpoint(endpoint);

        return IsFreeOfSecrets(safe) && RoundTrips(safe, endpoint.Scheme, host, port);
    }

    private static (Uri Endpoint, string Host, int Port) AdversarialEndpoint(bool useHttps, uint hostSeed, int portSeed)
    {
        var scheme = Scheme(useHttps);
        var host = $"host{hostSeed % 100_000}.example";
        var port = 1 + (int)((uint)portSeed % 65534);
        return (new Uri($"{scheme}://alice:topsecret@{host}:{port}/api/embed?token=query#fragment"), host, port);
    }

    private static string Scheme(bool useHttps) => useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

    private static bool IsFreeOfSecrets(string safe) =>
        !safe.Contains("alice", StringComparison.Ordinal)
            && !safe.Contains("topsecret", StringComparison.Ordinal)
            && !safe.Contains("query", StringComparison.Ordinal)
            && !safe.Contains("fragment", StringComparison.Ordinal);

    private static bool RoundTrips(string safe, string scheme, string host, int port) =>
        Uri.TryCreate(safe, UriKind.Absolute, out var parsed)
            && parsed.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase)
            && parsed.Host == host
            && parsed.Port == port;
}
