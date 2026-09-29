using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using LamuFlix.Api.ExceptionHandling;
using LamuFlix.ArchitectureTests.Fixtures.Pipeline;
using LamuFlix.ArchitectureTests.Fixtures.Valid;
using LamuFlix.ArchitectureTests.Fixtures.Valid.Features.MediaLibrary;
using LamuFlix.ArchitectureTests.Fixtures.Valid.Ports;
using LamuFlix.ArchitectureTests.Fixtures.Violating;
using LamuFlix.ArchitectureTests.Fixtures.Violating.Features.MediaLibrary;
using LamuFlix.Core;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Pipeline;
using NetArchTest.Rules;
using Xunit;
using ArchitectureResult = NetArchTest.Rules.TestResult;

namespace LamuFlix.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly Assembly Core = CoreAssembly.Instance;

    private static readonly Assembly Fixtures = typeof(SealedCommandFixture).Assembly;

    private const string PortWhitelistPattern =
        "^(IMovieRepository|IMovieCatalog|IMetadataProvider|IEnrichmentQueue|IMediaLibraryScanner|IMediaPlayerLauncher|IRecommendationCandidateSource|IPlaybackHistory)$";

    [Fact]
    public void Core_must_not_reference_entity_framework_core()
    {
        _ = CoreAssembly.Logger;
        _ = CoreAssembly.Categories;
        AssertNoDependency(Core, "Microsoft.EntityFrameworkCore");
        AssertNoDependency(Core, "EntityFrameworkCore");
    }

    [Fact]
    public void Core_must_not_reference_npgsql()
    {
        AssertNoDependency(Core, "Npgsql");
    }

    [Fact]
    public void Core_must_not_reference_rabbitmq()
    {
        AssertNoDependency(Core, "RabbitMQ");
    }

    [Fact]
    public void Core_must_not_reference_infrastructure()
    {
        AssertNoDependency(Core, "LamuFlix.Infrastructure");
    }

    [Fact]
    public void Core_must_not_reference_fluentvalidation_dependency_injection_or_opentelemetry()
    {
        AssertNoDependency(Core, "FluentValidation");
        AssertNoDependency(Core, "Microsoft.Extensions.DependencyInjection");
        AssertNoDependency(Core, "OpenTelemetry");
    }

    [Fact]
    public void Core_commands_must_be_sealed()
    {
        AssertSealed(Core, "Command", nameof(SealedCommandFixture), nameof(UnsealedCommandFixture));
    }

    [Fact]
    public void Core_queries_must_be_sealed()
    {
        AssertSealed(Core, "Query", nameof(SealedQueryFixture), nameof(UnsealedQueryFixture));
    }

    [Fact]
    public void Core_handlers_must_be_sealed()
    {
        AssertSealed(Core, "Handler", nameof(SealedHandlerFixture), nameof(UnsealedHandlerFixture));
    }

    [Fact]
    public void Core_features_must_depend_only_on_ports_domain_or_pipeline()
    {
        var coreViolations = FindPortViolations(
            Core,
            "LamuFlix.Core.Features",
            "LamuFlix.Core.Ports",
            "LamuFlix.Core.Domain",
            "LamuFlix.Core.Pipeline");
        Assert.True(coreViolations.Count == 0, string.Join(", ", coreViolations));

        var valid = FindPortViolations(
            Fixtures,
            "LamuFlix.ArchitectureTests.Fixtures.Valid.Features",
            "LamuFlix.ArchitectureTests.Fixtures.Valid.Ports",
            "LamuFlix.ArchitectureTests.Fixtures.Valid.Domain",
            "LamuFlix.ArchitectureTests.Fixtures.Valid.Pipeline");
        Assert.True(valid.Count == 0, string.Join(", ", valid));
        _ = new PortMediatedFixture(new StubMediaPort()).Port.Title;
        _ = new DirectFeatureCouplingFixture().User.Name;

        var violating = FindPortViolations(
            Fixtures,
            "LamuFlix.ArchitectureTests.Fixtures.Violating.Features",
            "LamuFlix.ArchitectureTests.Fixtures.Violating.Ports",
            "LamuFlix.ArchitectureTests.Fixtures.Violating.Domain",
            "LamuFlix.ArchitectureTests.Fixtures.Violating.Pipeline");
        Assert.Contains(nameof(DirectFeatureCouplingFixture), string.Join(", ", violating));
    }

    [Fact]
    public void Closed_handlers_must_be_sealed_and_requests_must_be_sealed_records()
    {
        AssertNoHandlerShapeViolations(Core);
        AssertNoHandlerShapeViolations(typeof(TracingDecorator<,>).Assembly);
        AssertNoHandlerShapeViolations(typeof(ValidationExceptionHandler).Assembly);

        var violations = FindHandlerShapeViolations(typeof(SealedRecordRequestCommandHandler).Assembly);
        Assert.Contains(
            violations,
            violation => violation.HandlerName == nameof(UnsealedRequestCommandHandler)
                && violation.Reason == "request is not sealed");
        Assert.Contains(
            violations,
            violation => violation.HandlerName == nameof(SealedNonRecordRequestCommandHandler)
                && violation.Reason == "request is not a record");
        Assert.DoesNotContain(
            violations,
            violation => violation.HandlerName == nameof(SealedNonRecordRequestCommandHandler)
                && violation.Reason == "request is not sealed");
        Assert.Contains(
            violations,
            violation => violation.HandlerName == nameof(UnsealedPipelineCommandHandler)
                && violation.Reason == "handler is not sealed");
        Assert.DoesNotContain(
            violations,
            violation => violation.HandlerName == nameof(SealedRecordRequestCommandHandler));
        Assert.Contains(
            violations,
            violation => violation.HandlerName == nameof(UnsealedRequestQueryHandler)
                && violation.Reason == "request is not sealed");
        Assert.DoesNotContain(
            violations,
            violation => violation.HandlerName == nameof(SealedRecordRequestQueryHandler));
    }

    [Fact]
    public void Core_ports_must_match_the_constitution_whitelist()
    {
        var ports = Types.InAssembly(Core)
            .That()
            .ResideInNamespace("LamuFlix.Core.Ports")
            .And()
            .AreInterfaces()
            .GetTypes()
            .ToList();

        Assert.True(ports.Count >= 6, $"Expected at least 6 ports, found {ports.Count}.");

        var result = Types.InAssembly(Core)
            .That()
            .ResideInNamespace("LamuFlix.Core.Ports")
            .And()
            .AreInterfaces()
            .Should()
            .HaveNameMatching(PortWhitelistPattern)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Core_must_not_depend_on_http_client_types()
    {
        var httpClientTypes = HttpClientTypeNames();
        Assert.NotEmpty(httpClientTypes);
        Assert.Contains("System.Net.Http.HttpClient", httpClientTypes);

        var result = Types.InAssembly(Core)
            .ShouldNot()
            .HaveDependencyOnAny(httpClientTypes)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Core_must_not_depend_on_process_or_process_start_info()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot()
            .HaveDependencyOnAny("System.Diagnostics.Process", "System.Diagnostics.ProcessStartInfo")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string[] HttpClientTypeNames()
    {
        return
        [
            .. typeof(HttpMessageInvoker).Assembly.GetExportedTypes()
                .Where(IsHttpClientType)
                .Select(static type => type.FullName)
                .OfType<string>(),
        ];
    }

    private static bool IsHttpClientType(Type type) =>
        typeof(HttpMessageInvoker).IsAssignableFrom(type) || typeof(HttpMessageHandler).IsAssignableFrom(type);

    private static void AssertNoDependency(Assembly assembly, string dependency)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(dependency)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static void AssertSealed(Assembly core, string namePattern, string validName, string violatingName)
    {
        var coreResult = Types.InAssembly(core)
            .That()
            .HaveNameMatching(namePattern)
            .And()
            .AreNotInterfaces()
            .Should()
            .BeSealed()
            .GetResult();
        Assert.True(coreResult.IsSuccessful, Describe(coreResult));

        var valid = Types.InAssembly(Fixtures)
            .That()
            .HaveName(validName)
            .Should()
            .BeSealed()
            .GetResult();
        Assert.True(valid.IsSuccessful, Describe(valid));

        var violating = Types.InAssembly(Fixtures)
            .That()
            .HaveName(violatingName)
            .Should()
            .BeSealed()
            .GetResult();
        Assert.False(violating.IsSuccessful);
    }

    private static IReadOnlyList<string> FindPortViolations(
        Assembly assembly,
        string featuresRoot,
        string portsNamespace,
        string domainNamespace,
        string pipelineNamespace)
    {
        return
        [
            .. from featureType in Types.InAssembly(assembly).That().ResideInNamespaceStartingWith(featuresRoot)
                .GetTypes()
            let ownNamespace = featureType.Namespace ??
                               throw new InvalidOperationException($"Feature type {featureType.Name} has no namespace.")
            let result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace(ownNamespace)
                .And()
                .HaveName(featureType.Name)
                .Should()
                .OnlyHaveDependenciesOn(ownNamespace, portsNamespace, domainNamespace, pipelineNamespace, "System",
                    "Microsoft.Extensions.Logging", "Microsoft.Extensions.Logging.Abstractions")
                .GetResult()
            where !result.IsSuccessful
            select featureType.FullName ?? featureType.Name
        ];
    }

    private sealed class StubMediaPort : IMediaPort
    {
        public string Title => "stub";
    }

    private static void AssertNoHandlerShapeViolations(Assembly assembly)
    {
        var violations = FindHandlerShapeViolations(assembly);
        Assert.True(violations.Count == 0, string.Join(", ", violations));
    }

    private static List<HandlerShapeViolation> FindHandlerShapeViolations(Assembly assembly)
    {
        var violations = new List<HandlerShapeViolation>();
        foreach (var type in DefinedTypes(assembly))
        {
            if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            {
                continue;
            }

            foreach (var handlerInterface in ClosedHandlerInterfaces(type))
            {
                CollectHandlerShapeViolations(violations, type, handlerInterface);
            }
        }

        return violations;
    }

    private static void CollectHandlerShapeViolations(
        List<HandlerShapeViolation> violations,
        Type handler,
        Type handlerInterface)
    {
        if (!handler.IsSealed)
        {
            violations.Add(new HandlerShapeViolation(handler.Name, "handler is not sealed"));
        }

        var request = handlerInterface.GenericTypeArguments[0];
        if (!request.IsSealed)
        {
            violations.Add(new HandlerShapeViolation(handler.Name, "request is not sealed"));
        }

        if (!IsRecord(request))
        {
            violations.Add(new HandlerShapeViolation(handler.Name, "request is not a record"));
        }
    }

    private static IEnumerable<Type> ClosedHandlerInterfaces(Type type)
    {
        return type.GetInterfaces().Where(candidate =>
            candidate.IsGenericType
            && !candidate.IsGenericTypeDefinition
            && (candidate.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                || candidate.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));
    }

    private static IEnumerable<Type> DefinedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;

    private readonly record struct HandlerShapeViolation(string HandlerName, string Reason);

    private static string Describe(ArchitectureResult result)
    {
        IReadOnlyList<string>? names = result.FailingTypeNames;
        return names is null ? "architecture rule failed" : string.Join(", ", names);
    }
}
