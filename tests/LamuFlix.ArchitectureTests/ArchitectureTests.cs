using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LamuFlix.ArchitectureTests.Fixtures.Valid;
using LamuFlix.ArchitectureTests.Fixtures.Valid.Features.MediaLibrary;
using LamuFlix.ArchitectureTests.Fixtures.Valid.Ports;
using LamuFlix.ArchitectureTests.Fixtures.Violating;
using LamuFlix.ArchitectureTests.Fixtures.Violating.Features.MediaLibrary;
using LamuFlix.Core;
using NetArchTest.Rules;
using Xunit;
using ArchitectureResult = NetArchTest.Rules.TestResult;

namespace LamuFlix.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly Assembly Core = CoreAssembly.Instance;

    private static readonly Assembly Fixtures = typeof(SealedCommandFixture).Assembly;

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

    private static string Describe(ArchitectureResult result)
    {
        IReadOnlyList<string>? names = result.FailingTypeNames;
        return names is null ? "architecture rule failed" : string.Join(", ", names);
    }
}
