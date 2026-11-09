using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Enrichment;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class LocalPlayListenerStartupTests
{
    private const string LoopbackHost = "127.0.0.1";
    private const string AnyHost = "0.0.0.0";
    private const string Ipv6AnyHost = "[::]";
    private const string Ipv6LoopbackHost = "[::1]";
    private const string RemoteHost = "10.0.0.5";

    [Fact]
    public async Task StartAsync_LocalPlayEnabledWithLoopbackUrl_SucceedsAndServesTheProvenListener()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            return settings;
        });

        // act
        var result = await host.StartAsync();
        var served = result.Started && await host.ServesRequestAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
        result.BoundAddresses.ShouldContain(address => address.Contains(LoopbackHost, StringComparison.Ordinal));
        served.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_LocalPlayDisabledWithWildcardUrl_ImposesNoBindRestriction()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(false));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{AnyHost}:{port}";
            return settings;
        });

        // act
        var result = await host.StartAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
    }

    [Theory]
    [InlineData(AnyHost)]
    [InlineData(Ipv6AnyHost)]
    [InlineData(RemoteHost)]
    [InlineData("devbox.invalid")]
    public async Task StartAsync_LocalPlayEnabledWithUnsafeUrl_RefusesStartupBeforeServing(string host)
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{host}:{port}";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();
        var served = result.Started && await fixture.ServesRequestAsync();
        var listening = LocalPlayRealKestrelHostFixture.IsListeningOn(fixture.ListenerPort);

        // assert
        result.Started.ShouldBeFalse();
        result.RefusedByValidator.ShouldBeTrue(result.Failure?.ToString());
        result.Failure.ShouldNotBeNull().Message.ShouldContain(nameof(FeatureOptions.LocalPlay));
        served.ShouldBeFalse();
        listening.ShouldBeFalse();
        fixture.Binder.BoundPorts.ShouldBeEmpty();
        fixture.Recorder.Entries.ShouldBe([LocalPlayRealKestrelHostFixture.BeforeValidatorProbeName]);
    }

    [Fact]
    public async Task StartAsync_MixedLoopbackAndWildcardUrlSet_RefusesStartup()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] =
                $"http://{LoopbackHost}:{port};http://{AnyHost}:{port + 1}";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();
        var served = result.Started && await fixture.ServesRequestAsync();

        // assert
        result.Started.ShouldBeFalse();
        result.RefusedByValidator.ShouldBeTrue(result.Failure?.ToString());
        served.ShouldBeFalse();
        fixture.Binder.BoundPorts.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_Ipv6LoopbackUrl_AcceptsTheListener()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{Ipv6LoopbackHost}:{port}";
            return settings;
        });

        // act
        var result = await host.StartAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
    }

    [Fact]
    public async Task StartAsync_NoUrlConfiguredAnywhere_AppliesTheDocumentedNet10DefaultInsteadOfRefusing()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(_ => settings);

        // act
        var result = await host.StartAsync();

        // assert
        result.RefusedByValidator.ShouldBeFalse(result.Failure?.ToString());
    }

    [Fact]
    public async Task StartAsync_HttpPortsShorthand_RefusesStartupBecauseTheShorthandBindsAWildcard()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.HttpPortsKey] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return settings;
        });

        // act
        var result = await fixture.StartAsync();

        // assert
        result.Started.ShouldBeFalse();
        result.RefusedByValidator.ShouldBeTrue(result.Failure?.ToString());
        fixture.Binder.BoundPorts.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_UrlOverridesHttpPortsShorthand_AcceptsTheLoopbackUrl()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            settings[WebHostDefaults.HttpPortsKey] = (port + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return settings;
        });

        // act
        var result = await host.StartAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
    }

    [Fact]
    public async Task StartAsync_KestrelEndpointSection_OverridesTheUrlFamilyAndRefusesAWildcardEndpoint()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            settings["Kestrel:Endpoints:Http:Url"] = $"http://{AnyHost}:{port}";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();

        // assert
        result.Started.ShouldBeFalse();
        result.RefusedByValidator.ShouldBeTrue(result.Failure?.ToString());
        fixture.Binder.BoundPorts.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_PreferHostingUrlsWithLoopbackUrl_OverridesTheWildcardKestrelEndpoint()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            settings[WebHostDefaults.PreferHostingUrlsKey] = "true";
            settings["Kestrel:Endpoints:Http:Url"] = $"http://{AnyHost}:{port}";
            return settings;
        });

        // act
        var result = await host.StartAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
    }

    [Fact]
    public async Task StartAsync_KestrelSectionWithUnprovableEndpointUrl_RefusesStartupRatherThanGuessing()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            settings["Kestrel:Endpoints:Http:Url"] = "not-a-listener";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();

        // assert
        result.Started.ShouldBeFalse();
        result.RefusedByValidator.ShouldBeTrue(result.Failure?.ToString());
    }

    [Fact]
    public async Task StartAsync_Refusal_RunsBeforeLaterExternallyActiveHostedServicesStart()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{AnyHost}:{port}";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();

        // assert
        result.Started.ShouldBeFalse();
        result.StartupSequence.ShouldBe([LocalPlayRealKestrelHostFixture.BeforeValidatorProbeName]);
        result.StartupSequence.ShouldNotContain(LocalPlayExternalWorkProbe.Name);
        fixture.Binder.BoundPorts.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_LoopbackListener_RunsLaterExternallyActiveHostedServicesAfterValidation()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var fixture = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            return settings;
        });

        // act
        var result = await fixture.StartAsync();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
        result.StartupSequence.ShouldBe(
            [LocalPlayRealKestrelHostFixture.BeforeValidatorProbeName, LocalPlayExternalWorkProbe.Name]);
        fixture.Binder.BoundPorts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task StartAsync_ValidatorObservesAnEmptyUnpopulatedAddressFeatureAndStillResolvesTheListener()
    {
        // arrange
        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(port =>
        {
            settings[WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:{port}";
            return settings;
        });

        // act
        var result = await host.StartAsync();
        var observation = host.AddressFeatureObservation;
        var reportedAfterStart = host.BoundAddresses();

        // assert
        result.Started.ShouldBeTrue(result.Failure?.ToString());
        observation.ShouldNotBeNull();
        observation.FeaturePresent.ShouldBeTrue();
        observation.Addresses.ShouldBeEmpty();
        observation.PreferHostingUrls.ShouldBeFalse();
        observation.ConfiguredUrls.ShouldBe($"http://{LoopbackHost}:{host.ListenerPort}");
        reportedAfterStart.ShouldContain(address => address.Contains(LoopbackHost, StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_NoUrlConfiguredAnywhere_MatchesTheDocumentedNet10DefaultListener()
    {
        // arrange
        var ambient = AmbientListenerSources();
        if (ambient.Count > 0)
        {
            Assert.Skip($"ambient listener configuration present ({string.Join(", ", ambient)}); the documented "
                + "empty-configuration default cannot be observed in this process");
        }

        var settings = new Dictionary<string, string?>(LocalPlayRealKestrelHostFixture.LocalPlaySettings(true));
        await using var host = LocalPlayRealKestrelHostFixture.Build(_ => settings);
        var documented = new Uri(LocalPlayListenerValidationHostedService.DocumentedDefaultAddress);

        // act
        var result = await host.StartAsync();

        // assert
        documented.IsLoopback.ShouldBeTrue();
        documented.Port.ShouldBe(5000);
        result.Started.ShouldBeTrue(result.Failure?.ToString());
        result.BoundAddresses.ShouldBe([LocalPlayListenerValidationHostedService.DocumentedDefaultAddress]);
    }

    [Fact]
    public async Task StartAsync_EmptyConfigurationWithAnEmptyAddressFeature_AppliesTheProveLoopbackDefault()
    {
        // arrange
        var validator = ValidatorHarness.Create(localPlay: true, configuration: []);

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        LocalPlayListenerValidationHostedService.DocumentedDefaultAddress.ShouldBe("http://localhost:5000");
        failure.ShouldBeNull();
    }

    [Fact]
    public async Task StartAsync_EmptyAddressFeatureWithAWildcardKestrelEndpoint_RefusesRatherThanTrustingTheEmptyFeature()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: true,
            configuration: new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Http:Url"] = "http://0.0.0.0:5000",
            });

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_PopulatedAddressFeatureWithAWildcardAddress_RefusesStartup()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: true,
            configuration: [],
            reportedAddresses: ["http://0.0.0.0:5000"]);

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_LocalPlayDisabledWithAnUnprovableListener_AddsNoBindRestriction()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: false,
            configuration: [],
            reportedAddresses: ["http://0.0.0.0:5000"]);

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeNull();
    }

    [Fact]
    public async Task StartAsync_ConfiguredAndReportedListenersContradict_RefusesStartup()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: true,
            configuration: new Dictionary<string, string?>
            {
                [WebHostDefaults.ServerUrlsKey] = $"http://{LoopbackHost}:5000",
            },
            reportedAddresses: ["http://0.0.0.0:6000"]);

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_PreferHostingUrlsWithoutAnyUrlFamilyInput_StillHonoursTheKestrelEndpointSection()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: true,
            configuration: new Dictionary<string, string?>
            {
                [WebHostDefaults.PreferHostingUrlsKey] = "true",
                ["Kestrel:Endpoints:Http:Url"] = "http://0.0.0.0:5000",
            });

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_KestrelEndpointSectionAbsent_FallsThroughToTheProvenLoopbackDefault()
    {
        // arrange
        var validator = ValidatorHarness.Create(
            localPlay: true,
            configuration: new Dictionary<string, string?>
            {
                [WebHostDefaults.PreferHostingUrlsKey] = "true",
            });

        // act
        var failure = await Record.ExceptionAsync(() => validator.StartAsync(TestContext.Current.CancellationToken));

        // assert
        failure.ShouldBeNull();
    }

    private static IReadOnlyList<string> AmbientListenerSources()
    {
        string[] candidates =
        [
            "ASPNETCORE_URLS",
            "ASPNETCORE_HTTP_PORTS",
            "ASPNETCORE_HTTPS_PORTS",
            "DOTNET_URLS",
            "URLS",
        ];
        return [.. candidates.Where(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))];
    }

    private sealed class ValidatorHarness : IServer
    {
        private ValidatorHarness(IServerAddressesFeature? feature)
        {
            var collection = new FeatureCollection();
            if (feature is not null)
            {
                collection.Set(feature);
            }

            Features = collection;
        }

        public IFeatureCollection Features { get; }

        public static LocalPlayListenerValidationHostedService Create(
            bool localPlay,
            IEnumerable<KeyValuePair<string, string?>> configuration,
            IReadOnlyList<string>? reportedAddresses = null)
        {
            var feature = new LocalPlayStubAddressesFeature(reportedAddresses ?? []);
            var configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(configuration)
                .Build();
            var features = Options.Create(new FeatureOptions { LocalPlay = localPlay });
            return new LocalPlayListenerValidationHostedService(
                features,
                configurationRoot,
                new ValidatorHarness(feature));
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }

        private sealed class LocalPlayStubAddressesFeature(IReadOnlyList<string> addresses) : IServerAddressesFeature
        {
            public ICollection<string> Addresses { get; } = [.. addresses];

            public bool PreferHostingUrls { get; set; }
        }
    }

    [Fact]
    public void Composition_ShippedHost_RegistersTheValidatorAheadOfTheSweeper()
    {
        // arrange
        using var factory = new ApiHostFactory(retainSweeper: true);

        // act
        var hostedServices = factory.Services.GetServices<IHostedService>().ToArray();
        var validatorIndex = Array.FindIndex(
            hostedServices,
            service => service is LocalPlayListenerValidationHostedService);
        var sweeperIndex = Array.FindIndex(
            hostedServices,
            service => service is StrandedMovieSweeper);

        // assert
        validatorIndex.ShouldBeGreaterThanOrEqualTo(0);
        sweeperIndex.ShouldBeGreaterThanOrEqualTo(0);
        validatorIndex.ShouldBeLessThan(sweeperIndex);
    }
}
