using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Infrastructure.Recommendations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiHostCompositionTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string LivenessRoute = "/health/live";
    private const string BusinessPrefix = "/api";
    private const string BrowseMoviesRoute = "/api/movies";
    private const string GetMovieDetailsRoute = "/api/movies/{id:int}";
    private const string DeleteMovieRoute = "/api/movies/{id:int}";
    private const string ImportMovieRoute = "/api/movies/import";
    private const string RequestEnrichmentRoute = "/api/movies/{id}/enrichment";
    private const string MatchImdbIdRoute = "/api/movies/{id}/imdb";
    private const string WatchlistRoute = "/api/movies/{id}/watchlist";
    private const string WatchedRoute = "/api/movies/{id}/watched";
    private const string NotInterestedRoute = "/api/movies/{id}/not-interested";
    private const string PlayMovieRoute = "/api/movies/{id}/play";
    private const string GetGenresRoute = "/api/genres";
    private const string GetCollectionsRoute = "/api/collections";
    private const string GetPeopleRoute = "/api/people";
    private const string GetDirectorsRoute = "/api/directors";
    private const string GetRecommendationsRoute = "/api/movies/{id:int}/recommendations";
    private const string ForYouRecommendationsRoute = "/api/recommendations/for-you";

    public static TheoryData<Type, Type, Type> ManifestContracts =>
        new()
        {
            {
                typeof(ICommandHandler<RequestEnrichmentCommand, MovieId>),
                typeof(RequestEnrichmentCommand),
                typeof(MovieId)
            },
            {
                typeof(ICommandHandler<ImportMovieFolderCommand, MovieId>),
                typeof(ImportMovieFolderCommand),
                typeof(MovieId)
            },
            {
                typeof(IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>),
                typeof(BrowseMoviesQuery),
                typeof(PagedResult<MovieSummary>)
            },
            {
                typeof(IQueryHandler<GetMovieDetailsQuery, MovieDetails>),
                typeof(GetMovieDetailsQuery),
                typeof(MovieDetails)
            },
            {
                typeof(ICommandHandler<PlayMovieCommand, Unit>),
                typeof(PlayMovieCommand),
                typeof(Unit)
            },
            {
                typeof(ICommandHandler<AddToWatchlistCommand, Unit>),
                typeof(AddToWatchlistCommand),
                typeof(Unit)
            },
            {
                typeof(ICommandHandler<RemoveFromWatchlistCommand, Unit>),
                typeof(RemoveFromWatchlistCommand),
                typeof(Unit)
            },
            {
                typeof(IQueryHandler<GetGenresQuery, IReadOnlyList<GenreFacet>>),
                typeof(GetGenresQuery),
                typeof(IReadOnlyList<GenreFacet>)
            },
            {
                typeof(IQueryHandler<GetCollectionsQuery, IReadOnlyList<CollectionFacet>>),
                typeof(GetCollectionsQuery),
                typeof(IReadOnlyList<CollectionFacet>)
            },
            {
                typeof(IQueryHandler<GetPeopleQuery, IReadOnlyList<PersonFacet>>),
                typeof(GetPeopleQuery),
                typeof(IReadOnlyList<PersonFacet>)
            },
            {
                typeof(IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>),
                typeof(GetRecommendationsQuery),
                typeof(ImmutableList<RecommendedMovie>)
            },
            {
                typeof(IQueryHandler<GetForYouRecommendationsQuery, ImmutableList<RecommendedMovie>>),
                typeof(GetForYouRecommendationsQuery),
                typeof(ImmutableList<RecommendedMovie>)
            },
        };

    [Fact]
    public async Task Services_StartedComposition_ServesARequestThroughTheRealMiddlewarePipeline()
    {
        // arrange
        using var client = factory.CreateClient();

        // act
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public void Services_StartedComposition_ResolvesTheCatalogAndTheScannerFromOneScope()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var catalog = scope.ServiceProvider.GetRequiredService<IMovieCatalog>();
        var scanner = scope.ServiceProvider.GetRequiredService<IMediaLibraryScanner>();

        // assert
        catalog.ShouldBeOfType<EfMovieCatalog>();
        scanner.ShouldBeOfType<DirectoryMediaLibraryScanner>();
    }

    [Fact]
    public void Persistence_StartedComposition_ResolvesAScopedCandidateSourceWithTheCalculatorAndBoundOptions()
    {
        // arrange
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        // act
        var source = firstScope.ServiceProvider.GetRequiredService<IRecommendationCandidateSource>();
        var sameScopeSource = firstScope.ServiceProvider.GetRequiredService<IRecommendationCandidateSource>();
        var otherScopeSource = secondScope.ServiceProvider.GetRequiredService<IRecommendationCandidateSource>();
        var calculator = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var options = firstScope.ServiceProvider.GetRequiredService<IOptions<RecommendationOptions>>();

        // assert
        source.ShouldBeOfType<EfRecommendationCandidateSource>();
        source.ShouldBeSameAs(sameScopeSource);
        source.ShouldNotBeSameAs(otherScopeSource);
        calculator.ShouldBeOfType<GenreWeightsCalculator>();
        options.Value.ShouldNotBeNull();
        options.Value.Weights.ShouldNotBeNull();
    }

    [Fact]
    public void Services_StartedComposition_ResolvesTheRealFileSystemAndTheInheritedClock()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // assert
        fileSystem.ShouldBeOfType<FileSystem>();
        time.ShouldBe(TimeProvider.System);
    }

    [Fact]
    public void ApiAssembly_HasNoMvcOrRazorReference()
    {
        // arrange
        var references = typeof(Program).Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty);

        // act
        var forbidden = references.Where(IsMvcOrRazor).ToList();

        // assert
        forbidden.ShouldBeEmpty();
    }

    [Fact]
    public void Routes_StartedComposition_ExposeTheLivenessRouteAndBothLibraryBusinessRoutes()
    {
        // arrange
        var dataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        // act
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToList();

        // assert
        routes.Where(route => string.Equals(route, LivenessRoute, StringComparison.Ordinal))
            .ShouldHaveSingleItem();
        routes.Where(route => route.StartsWith(BusinessPrefix, StringComparison.Ordinal))
            .ShouldBe(
                [
                    BrowseMoviesRoute,
                    GetMovieDetailsRoute,
                    DeleteMovieRoute,
                    ImportMovieRoute,
                    RequestEnrichmentRoute,
                    MatchImdbIdRoute,
                    WatchlistRoute,
                    WatchlistRoute,
                    PlayMovieRoute,
                    GetGenresRoute,
                    GetCollectionsRoute,
                    GetPeopleRoute,
                    GetDirectorsRoute,
                    WatchedRoute,
                    WatchedRoute,
                    NotInterestedRoute,
                    NotInterestedRoute,
                    GetRecommendationsRoute,
                    ForYouRecommendationsRoute,
                ]);
    }

    [Theory]
    [MemberData(nameof(ManifestContracts))]
    public void Handlers_StartedComposition_ResolveThroughTheOutermostTracingDecorator(
        Type contract,
        Type request,
        Type response)
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var resolved = scope.ServiceProvider.GetRequiredService(contract);

        // assert
        resolved.GetType().ShouldBe(typeof(TracingDecorator<,>).MakeGenericType(request, response));
    }

    [Fact]
    public void Validators_StartedComposition_ResolveTheRecommendationsValidatorThroughThePipeline()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<GetRecommendationsQuery>>();

        // assert
        validator.ShouldBeOfType<GetRecommendationsQueryValidator>();
    }

    [Fact]
    public void HandlerChain_StartedComposition_ComposesTracingLoggingValidationThenTheHandler()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var outermost = scope.ServiceProvider
            .GetRequiredService<IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>>();
        var logging = NextInChain<BrowseMoviesQuery, PagedResult<MovieSummary>>(outermost);
        var validation = NextInChain<BrowseMoviesQuery, PagedResult<MovieSummary>>(logging);
        var composed = ComposedHandler<BrowseMoviesQuery, PagedResult<MovieSummary>, BrowseMoviesQueryHandler>(outermost);

        // assert
        outermost.ShouldBeOfType<TracingDecorator<BrowseMoviesQuery, PagedResult<MovieSummary>>>();
        logging.ShouldBeOfType<LoggingDecorator<BrowseMoviesQuery, PagedResult<MovieSummary>>>();
        validation.ShouldBeOfType<ValidationDecorator<BrowseMoviesQuery, PagedResult<MovieSummary>>>();
        composed.ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<BrowseMoviesQueryHandler>());
    }

    [Fact]
    public void Handlers_RecoveryComposition_ResolvesDecoratedSweepAndRequeueHandlersAndRetainsSweeper()
    {
        // arrange
        using var recoveryFactory = new ApiHostFactory(retainSweeper: true);
        using var scope = recoveryFactory.Services.CreateScope();

        // act
        var sweep = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<SweepStrandedMoviesCommand, int>>();
        var requeue = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<RequeueStrandedMoviesCommand, int>>();
        var registrations = recoveryFactory.Services.GetServices<IHostedService>();

        // assert
        sweep.ShouldBeOfType<TracingDecorator<SweepStrandedMoviesCommand, int>>();
        requeue.ShouldBeOfType<TracingDecorator<RequeueStrandedMoviesCommand, int>>();
        registrations.ShouldContain(service => service is StrandedMovieSweeper);
    }

    [Fact]
    public void Services_StartedComposition_RegistersTheFeaturesConsumerAndItsReadyHealthCheckWithoutMaskingMetadataLiveness()
    {
        // arrange
        using var healthFactory = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);

        // act
        var hostedServices = healthFactory.Services.GetServices<IHostedService>().ToArray();
        var registrations = healthFactory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        hostedServices.ShouldContain(service => service is FeaturesConsumer);
        hostedServices.ShouldContain(service => service is EnrichmentConsumer);
        healthFactory.Services.GetRequiredService<FeaturesConsumerLiveness>().ShouldNotBeNull();
        registrations.ShouldContain(registration =>
            registration.Name == "features-consumer" && registration.Tags.Contains(HealthCheckTags.Ready));
        registrations.ShouldContain(registration =>
            registration.Name == "enrichment-consumer" && registration.Tags.Contains(HealthCheckTags.Ready));
    }

    [Fact]
    public void Services_RelayComposition_RegistersExactlyOneWorkerAndItsReadyCheckAndRetainsTheSweeper()
    {
        // arrange
        using var relayFactory = new ApiHostFactory(
            retainSweeper: true,
            retainHealthChecks: true,
            retainOutboxRelay: true);

        // act
        var hostedServices = relayFactory.Services.GetServices<IHostedService>().ToArray();
        var registrations = relayFactory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        hostedServices.Where(service => service is OutboxRelayHostedService).ShouldHaveSingleItem();
        hostedServices.ShouldContain(service => service is StrandedMovieSweeper);
        var relayRegistration = registrations
            .Where(registration => registration.Name == OutboxRelayHealthCheck.HealthCheckName)
            .ShouldHaveSingleItem();
        relayRegistration.Tags.ShouldContain(HealthCheckTags.Ready);
    }

    [Fact]
    public void UnrelatedHost_RemovesOnlyTheSweeper()
    {
        // arrange
        var hostedServices = factory.Services.GetServices<IHostedService>().ToArray();

        // assert
        hostedServices.ShouldNotContain(service => service is StrandedMovieSweeper);
        hostedServices.ShouldNotContain(service => service is OutboxRelayHostedService);
        hostedServices.ShouldContain(service => service is EnrichmentConsumer);
        hostedServices.ShouldContain(service => service is FeaturesConsumer);
    }

    [Theory]
    [InlineData(nameof(EnrichmentOptions.ClaimLease))]
    [InlineData(nameof(EnrichmentOptions.SweepInterval))]
    public void Startup_NonpositiveSweeperDuration_FailsWithTheConfigurationKey(string key)
    {
        // arrange
        var overrides = new Dictionary<string, string?>
        {
            [$"{EnrichmentOptions.SectionName}:{key}"] = "00:00:00",
        };
        using var invalidFactory = new ApiHostFactory(retainSweeper: false, additionalSettings: overrides);

        // act
        var failure = Should.Throw<Exception>(() => invalidFactory.CreateClient());

        // assert
        failure.ToString().ShouldContain($"{EnrichmentOptions.SectionName}:{key}");
    }

    [Fact]
    public void PlayRow_LocalPlayDisabled_ComposesAroundTheDisabledLauncher()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var outermost = scope.ServiceProvider.GetRequiredService<ICommandHandler<PlayMovieCommand, Unit>>();
        var composed = ComposedHandler<PlayMovieCommand, Unit, PlayMovieCommandHandler>(outermost);

        // assert
        outermost.ShouldBeOfType<TracingDecorator<PlayMovieCommand, Unit>>();
        ReadField<IMediaPlayerLauncher>(composed).ShouldBeOfType<DisabledMediaPlayerLauncher>();
    }

    private static object NextInChain<TReq, TRes>(object decorator)
        where TReq : class =>
        ReadField<Func<TReq, CancellationToken, Task<TRes>>>(decorator).Target.ShouldNotBeNull();

    private static THandler ComposedHandler<TReq, TRes, THandler>(object outermost)
        where TReq : class =>
        ReadField<THandler>(
            NextInChain<TReq, TRes>(NextInChain<TReq, TRes>(NextInChain<TReq, TRes>(outermost))));

    private static TField ReadField<TField>(object instance) =>
        (TField)instance.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single(candidate => candidate.FieldType == typeof(TField))
            .GetValue(instance)
            .ShouldNotBeNull();

    private static bool IsMvcOrRazor(string referenceName) =>
        referenceName.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal)
        || referenceName.Contains("Razor", StringComparison.Ordinal);
}
