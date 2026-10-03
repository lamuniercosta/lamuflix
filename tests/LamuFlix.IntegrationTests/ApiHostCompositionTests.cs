using System;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiHostCompositionTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
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

    private static bool IsMvcOrRazor(string referenceName) =>
        referenceName.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal)
        || referenceName.Contains("Razor", StringComparison.Ordinal);
}
