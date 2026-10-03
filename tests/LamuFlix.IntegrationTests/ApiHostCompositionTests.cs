using System;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.Playback;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiHostCompositionTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string LivenessRoute = "/health/live";
    private const string BusinessPrefix = "/api";

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
    public void Routes_StartedComposition_ExposeTheLivenessRouteAndNoBusinessPrefix()
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
        routes.Where(route => route.StartsWith(BusinessPrefix, StringComparison.Ordinal)).ShouldBeEmpty();
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
