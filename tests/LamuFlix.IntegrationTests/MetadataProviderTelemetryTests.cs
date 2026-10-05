using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(MetadataProviderCollection.Name)]
public sealed class MetadataProviderTelemetryTests(MetadataProviderProbe probe)
    : IClassFixture<MetadataProviderProbe>, IAsyncLifetime
{
    private const string FoundBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "A blade runner must pursue and terminate four replicants.",
          "imdbRating": "8.1",
          "imdbID": "tt0083658"
        }
        """;

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_AgainstTheStub_EmitsRedactedHttpClientTelemetryWithoutTheSentinel()
    {
        // arrange
        StubFound();
        var services = probe.BuildServices();
        var stub = new Uri(probe.Server.Urls[0]);
        var sentinel = MetadataProviderProbe.SentinelApiKey;
        var escaped = Uri.EscapeDataString(sentinel);

        // act
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName, "System.Net.Http");
        var result = await services.GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();

        var httpClientLogs = probe.Logs.Entries.Where(IsHttpClientHandlerLog).ToList();
        httpClientLogs.Count.ShouldBeGreaterThan(0);
        httpClientLogs.ShouldContain(entry => CarriesRedactedStubUri(entry, stub, sentinel));

        capture.Activities.ShouldContain(activity =>
            activity.Kind == ActivityKind.Client
            && string.Equals(activity.Source.Name, "System.Net.Http", StringComparison.Ordinal)
            && MentionsHost(activity, stub.Host));
        capture.Activities.ShouldContain(activity =>
            string.Equals(activity.Source.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal)
            && string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal));

        var activityCorpus = string.Join("\n", capture.Activities.SelectMany(ActivityFragments));
        activityCorpus.ShouldNotContain(sentinel);
        activityCorpus.ShouldNotContain(escaped);

        var logCorpus = string.Join("\n", probe.Logs.Entries.SelectMany(LogFragments));
        logCorpus.ShouldNotContain(sentinel);
        logCorpus.ShouldNotContain(escaped);
    }

    private void StubFound() =>
        probe.Server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));

    private static bool IsHttpClientHandlerLog(MetadataProviderProbe.CapturedLog entry) =>
        entry.CategoryName.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal)
        && (entry.CategoryName.EndsWith(".LogicalHandler", StringComparison.Ordinal)
            || entry.CategoryName.EndsWith(".ClientHandler", StringComparison.Ordinal));

    private static bool CarriesRedactedStubUri(
        MetadataProviderProbe.CapturedLog entry,
        Uri stub,
        string sentinel)
    {
        foreach (var value in NamedValues(entry, "Uri"))
        {
            var text = value?.ToString() ?? string.Empty;
            if (ContainsStubLocation(text, stub) && IsRedacted(text, sentinel))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsStubLocation(string text, Uri stub) =>
        text.Contains(stub.Host, StringComparison.Ordinal)
        && text.Contains(stub.AbsolutePath, StringComparison.Ordinal);

    private static bool IsRedacted(string text, string sentinel) =>
        !text.Contains("apikey", StringComparison.Ordinal)
        && !text.Contains(sentinel, StringComparison.Ordinal);

    private static IEnumerable<object?> NamedValues(MetadataProviderProbe.CapturedLog entry, string name)
    {
        foreach (var pair in entry.State)
        {
            if (string.Equals(pair.Key, name, StringComparison.Ordinal))
            {
                yield return pair.Value;
            }
        }

        foreach (var scope in entry.Scopes)
        {
            if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                continue;
            }

            foreach (var pair in pairs)
            {
                if (string.Equals(pair.Key, name, StringComparison.Ordinal))
                {
                    yield return pair.Value;
                }
            }
        }
    }

    private static bool MentionsHost(Activity activity, string host) =>
        ActivityFragments(activity).Any(fragment => fragment.Contains(host, StringComparison.Ordinal));

    private static IEnumerable<string> ActivityFragments(Activity activity)
    {
        yield return activity.DisplayName;
        yield return activity.OperationName;
        yield return activity.StatusDescription ?? string.Empty;
        foreach (var tag in activity.TagObjects)
        {
            yield return tag.Key;
            yield return tag.Value?.ToString() ?? string.Empty;
        }

        foreach (var activityEvent in activity.Events)
        {
            yield return activityEvent.Name;
            foreach (var tag in activityEvent.Tags)
            {
                yield return tag.Key;
                yield return tag.Value?.ToString() ?? string.Empty;
            }
        }
    }

    private static IEnumerable<string> LogFragments(MetadataProviderProbe.CapturedLog entry)
    {
        yield return entry.CategoryName;
        yield return entry.Message;
        yield return entry.ExceptionText;
        foreach (var pair in entry.State)
        {
            yield return pair.Key;
            yield return pair.Value?.ToString() ?? string.Empty;
        }

        foreach (var scope in entry.Scopes)
        {
            yield return scope.ToString() ?? string.Empty;
            if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                continue;
            }

            foreach (var pair in pairs)
            {
                yield return pair.Key;
                yield return pair.Value?.ToString() ?? string.Empty;
            }
        }
    }

    private sealed class ActivityCapture : IDisposable
    {
        private readonly ConcurrentBag<Activity> activities = [];
        private readonly ActivityListener listener;

        public ActivityCapture(params string[] sourceNames)
        {
            listener = new ActivityListener
            {
                ShouldListenTo = source => Array.IndexOf(sourceNames, source.Name) >= 0,
                Sample = AlwaysSample,
                ActivityStopped = activity => activities.Add(activity),
            };
            ActivitySource.AddActivityListener(listener);
        }

        private static ActivitySamplingResult AlwaysSample(ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllData;

        public IReadOnlyList<Activity> Activities => [.. activities];

        public void Dispose() => listener.Dispose();
    }
}
