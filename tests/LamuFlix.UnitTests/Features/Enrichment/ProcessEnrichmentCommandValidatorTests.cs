using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Infrastructure.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ProcessEnrichmentCommandValidatorTests
{
    [Fact]
    public async Task Validate_ValidCommand_HasNoFailures()
    {
        // arrange
        var validator = new ProcessEnrichmentCommandValidator();

        // act
        var result = await validator.ValidateAsync(new ProcessEnrichmentCommand(new MovieId(7), 1), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_DefaultMovieId_IsInvalid()
    {
        // arrange
        var validator = new ProcessEnrichmentCommandValidator();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - the validator must reject a null movie id.
        var result = await validator.ValidateAsync(new ProcessEnrichmentCommand(null!, 1), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(ProcessEnrichmentCommand.MovieId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Validate_AttemptBelowTheFloor_IsInvalid(int attempt)
    {
        // arrange
        var validator = new ProcessEnrichmentCommandValidator();

        // act
        var result = await validator.ValidateAsync(new ProcessEnrichmentCommand(new MovieId(7), attempt), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(ProcessEnrichmentCommand.Attempt));
    }

    [Fact]
    public async Task Validate_IsResolvedByTheServiceCollection()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<ProcessEnrichmentCommand>, ProcessEnrichmentCommandValidator>();
        await using var provider = services.BuildServiceProvider();

        // act
        var resolved = provider.GetRequiredService<IValidator<ProcessEnrichmentCommand>>();

        // assert
        resolved.ShouldNotBeNull();
    }
}
