using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class LocalPlayStartupRecorder
{
    private readonly List<string> entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    public void Record(string entry)
    {
        lock (entries)
        {
            entries.Add(entry);
        }
    }
}

public sealed class LocalPlayOrderProbe(LocalPlayStartupRecorder recorder, string name) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recorder.Record(name);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class LocalPlayExternalWorkProbe(LocalPlayStartupRecorder recorder, SocketBinder binder) : IHostedService
{
    public const string Name = "externally-active-service";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        recorder.Record(Name);
        binder.Bind();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed record LocalPlayAddressFeatureObservation(
    bool FeaturePresent,
    IReadOnlyList<string> Addresses,
    bool PreferHostingUrls,
    string? ConfiguredUrls);

public sealed class LocalPlayAddressFeatureProbe(
    IServer server,
    IConfiguration configuration) : IHostedService
{
    public const string Name = "address-feature-probe";

    public LocalPlayAddressFeatureObservation? Observation { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var feature = server.Features.Get<IServerAddressesFeature>();
        Observation = new LocalPlayAddressFeatureObservation(
            feature is not null,
            [.. feature?.Addresses ?? []],
            feature?.PreferHostingUrls ?? false,
            configuration[WebHostDefaults.ServerUrlsKey]);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class SocketBinder(int port) : IDisposable
{
    private readonly List<int> boundPorts = [];
    private TcpListener? listener;

    public IReadOnlyList<int> BoundPorts => boundPorts;

    public void Bind()
    {
        listener ??= new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        boundPorts.Add(port);
    }

    public void Dispose()
    {
        listener?.Stop();
        listener = null;
    }
}

public sealed record LocalPlayRealKestrelStartResult(
    bool Started,
    Exception? Failure,
    IReadOnlyList<string> StartupSequence,
    IReadOnlyList<string> BoundAddresses)
{
    public bool RefusedByValidator => Failure is InvalidOperationException;
}

public sealed class LocalPlayRealKestrelHostFixture : IAsyncDisposable
{
    public const string BeforeValidatorProbeName = "probe-before-validator";

    private static readonly HttpClient SharedHttpClient = new();

    private static readonly string[] ListenerKeys =
    [
        WebHostDefaults.ServerUrlsKey,
        WebHostDefaults.HttpPortsKey,
        WebHostDefaults.HttpsPortsKey,
        WebHostDefaults.PreferHostingUrlsKey,
    ];

    private readonly WebApplication app;

    private LocalPlayRealKestrelHostFixture(
        WebApplication app,
        int listenerPort,
        LocalPlayStartupRecorder recorder,
        SocketBinder binder)
    {
        this.app = app;
        ListenerPort = listenerPort;
        Recorder = recorder;
        Binder = binder;
    }

    public int ListenerPort { get; }

    public LocalPlayStartupRecorder Recorder { get; }

    public SocketBinder Binder { get; }

    public LocalPlayAddressFeatureObservation? AddressFeatureObservation =>
        app.Services.GetRequiredService<LocalPlayAddressFeatureProbe>().Observation;

    private static void ApplyHostLevelListenerSettings(
        WebApplicationBuilder builder,
        IEnumerable<KeyValuePair<string, string?>> settings)
    {
        var values = settings.ToDictionary(setting => setting.Key, setting => setting.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        foreach (var key in ListenerKeys)
        {
            builder.WebHost.UseSetting(key, values.TryGetValue(key, out var value) ? value : string.Empty);
        }
    }

    public static LocalPlayRealKestrelHostFixture Build(Func<int, IEnumerable<KeyValuePair<string, string?>>> settingsFactory)
    {
        ArgumentNullException.ThrowIfNull(settingsFactory);
        var listenerPort = ReserveFreePort();
        var recorder = new LocalPlayStartupRecorder();
        var binder = new SocketBinder(ReserveFreePort());
        var settings = settingsFactory(listenerPort).ToArray();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(LocalPlayListenerValidationHostedService).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.AddInMemoryCollection(settings);
        ApplyHostLevelListenerSettings(builder, settings);
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel();
        builder.Services.AddOptions<FeatureOptions>().BindConfiguration(FeatureOptions.SectionName);
        builder.Services.AddSingleton(recorder);
        builder.Services.AddSingleton(binder);
        builder.Services.AddSingleton<LocalPlayAddressFeatureProbe>();
        builder.Services.AddHostedService(provider => new LocalPlayOrderProbe(
            provider.GetRequiredService<LocalPlayStartupRecorder>(),
            BeforeValidatorProbeName));
        builder.Services.AddHostedService<LocalPlayListenerValidationHostedService>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<LocalPlayAddressFeatureProbe>());
        builder.Services.AddHostedService(provider => new LocalPlayExternalWorkProbe(
            provider.GetRequiredService<LocalPlayStartupRecorder>(),
            provider.GetRequiredService<SocketBinder>()));
        var app = builder.Build();
        app.MapGet("/", static () => "ok");
        return new LocalPlayRealKestrelHostFixture(app, listenerPort, recorder, binder);
    }

    public async Task<LocalPlayRealKestrelStartResult> StartAsync()
    {
        try
        {
            await app.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return new LocalPlayRealKestrelStartResult(false, failure, Recorder.Entries, []);
        }

        return new LocalPlayRealKestrelStartResult(true, null, Recorder.Entries, BoundAddresses());
    }

    public IReadOnlyList<string> BoundAddresses()
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        return [.. feature?.Addresses ?? []];
    }

    public async Task<bool> ServesRequestAsync()
    {
        try
        {
            using var response = await SharedHttpClient.GetAsync(
                new Uri($"http://127.0.0.1:{ListenerPort}/"),
                TestContext.Current.CancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
    public async ValueTask DisposeAsync()
    {
        await app.DisposeAsync();
        Binder.Dispose();
    }

    private static int ReserveFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static bool IsListeningOn(int port)
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static Dictionary<string, string?> LocalPlaySettings(bool enabled) =>
        new()
        {
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = enabled ? "true" : "false",
        };
}
