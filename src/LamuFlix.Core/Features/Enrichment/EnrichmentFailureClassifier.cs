using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public static class EnrichmentFailureClassifier
{
    public static EnrichmentFailureCategory Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        foreach (var node in Flatten(exception))
        {
            var category = MapNode(node);
            if (category is not null)
            {
                return category;
            }
        }

        return EnrichmentFailureCategory.Unknown;
    }

    private static EnrichmentFailureCategory? MapNode(Exception node)
    {
        if (node is HttpRequestException http)
        {
            return MapHttpStatus(ReadStatusCode(http));
        }

        return MapTransport(node);
    }

    private static int? ReadStatusCode(HttpRequestException http) =>
        http.StatusCode is { } status ? (int)status : null;

    private static EnrichmentFailureCategory MapHttpStatus(int? statusCode)
    {
        if (statusCode is null)
        {
            return EnrichmentFailureCategory.ProviderUnavailable;
        }

        return MapPresentStatus(statusCode.Value);
    }

    private static EnrichmentFailureCategory MapPresentStatus(int statusCode)
    {
        if (statusCode == 429)
        {
            return EnrichmentFailureCategory.RateLimited;
        }

        if (IsUnavailableStatus(statusCode))
        {
            return EnrichmentFailureCategory.ProviderUnavailable;
        }

        if (IsInvalidClientStatus(statusCode))
        {
            return EnrichmentFailureCategory.InvalidResponse;
        }

        return EnrichmentFailureCategory.Unknown;
    }

    private static bool IsUnavailableStatus(int statusCode) =>
        statusCode == 408 || statusCode is >= 500 and <= 599;

    private static bool IsInvalidClientStatus(int statusCode) =>
        statusCode is >= 400 and <= 499;

    private static EnrichmentFailureCategory? MapTransport(Exception node)
    {
        if (IsProviderTransport(node))
        {
            return EnrichmentFailureCategory.ProviderUnavailable;
        }

        if (node is JsonException)
        {
            return EnrichmentFailureCategory.InvalidResponse;
        }

        if (node is OperationCanceledException canceled)
        {
            return MapCancellation(canceled);
        }

        return null;
    }

    private static bool IsProviderTransport(Exception node) =>
        node is TimeoutException or SocketException or IOException;

    private static EnrichmentFailureCategory? MapCancellation(OperationCanceledException canceled)
    {
        if (canceled.InnerException is TimeoutException)
        {
            return EnrichmentFailureCategory.ProviderUnavailable;
        }

        if (canceled.CancellationToken.IsCancellationRequested)
        {
            ExceptionDispatchInfo.Capture(canceled).Throw();
        }

        return null;
    }

    private static List<Exception> Flatten(Exception root)
    {
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        var order = new List<Exception>();
        var pending = new Stack<Exception>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!visited.Add(node))
            {
                continue;
            }

            order.Add(node);
            PushChildren(pending, node);
        }

        return order;
    }

    private static void PushChildren(Stack<Exception> pending, Exception node)
    {
        if (node is AggregateException aggregate)
        {
            PushAggregateChildren(pending, aggregate);
            return;
        }

        if (node.InnerException is { } inner)
        {
            pending.Push(inner);
        }
    }

    private static void PushAggregateChildren(Stack<Exception> pending, AggregateException aggregate)
    {
        for (var index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
        {
            pending.Push(aggregate.InnerExceptions[index]);
        }
    }
}
