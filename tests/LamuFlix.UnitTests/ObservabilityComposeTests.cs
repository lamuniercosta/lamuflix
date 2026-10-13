using System;
using System.IO;

namespace LamuFlix.UnitTests;

public sealed class ObservabilityComposeTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string ComposeFileName = "docker-compose.observability.yml";
    private const string DashboardImage = "image: mcr.microsoft.com/dotnet/aspire-dashboard:13.6";
    private const string DashboardUiPort = "\"18888:18888\"";
    private const string OtlpGrpcPort = "\"4317:18889\"";
    private const string OtlpHttpPort = "\"4318:18890\"";
    private const string UnsecuredFrontend = "DASHBOARD__FRONTEND__AUTHMODE=Unsecured";

    [Fact]
    public void ComposeFile_DeclaresAspireDashboardService()
    {
        // arrange
        var compose = ReadComposeFile();

        // act
        var declaresService = compose.Contains("\n  aspire-dashboard:\n", StringComparison.Ordinal);

        // assert
        declaresService.ShouldBeTrue();
    }

    [Fact]
    public void ComposeFile_PinsDashboardImageToVersion13_6()
    {
        // arrange
        var compose = ReadComposeFile();

        // act
        var contains = compose.Contains(DashboardImage, StringComparison.Ordinal);

        // assert
        contains.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DashboardUiPort)]
    [InlineData(OtlpGrpcPort)]
    [InlineData(OtlpHttpPort)]
    public void ComposeFile_PublishesDashboardAndOtlpPorts(string mapping)
    {
        // arrange
        var compose = ReadComposeFile();

        // act
        var contains = compose.Contains(mapping, StringComparison.Ordinal);

        // assert
        contains.ShouldBeTrue();
    }

    [Fact]
    public void ComposeFile_RunsDashboardUnsecuredForLocalUseOnly()
    {
        // arrange
        var compose = ReadComposeFile();

        // act
        var contains = compose.Contains(UnsecuredFrontend, StringComparison.Ordinal);

        // assert
        contains.ShouldBeTrue();
    }

    private static string ReadComposeFile()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, ComposeFileName));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; current is not null && depth < RootWalkLimit; depth++, current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, RootMarkerFileName)))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException($"No {RootMarkerFileName} found above {AppContext.BaseDirectory}.");
    }
}
