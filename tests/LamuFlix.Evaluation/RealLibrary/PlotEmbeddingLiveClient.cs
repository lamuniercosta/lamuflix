using System;
using System.Net.Http;

namespace LamuFlix.Evaluation.RealLibrary;

public static class PlotEmbeddingLiveClient
{
    public const string EmbeddingsPath = "/api/embed";

    public const string NonLocalEndpointMessage =
        "Configured Ollama endpoint is not local; refusing remote embedding requests.";

    public static Uri EmbeddingsUri(string? endpoint)
    {
        if (!PlotEmbeddingEndpointPolicy.IsLocalEndpoint(endpoint))
        {
            throw new InvalidOperationException(NonLocalEndpointMessage);
        }

        return new Uri(new Uri(endpoint!), EmbeddingsPath);
    }

    public static SocketsHttpHandler CreateHandler(string? endpoint)
    {
        _ = EmbeddingsUri(endpoint);

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
        };
    }

    public static HttpClient CreateClient(string? endpoint)
    {
        var handler = CreateHandler(endpoint);
        return new HttpClient(handler, disposeHandler: true);
    }
}
