using System;
using System.Collections.Generic;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleDiagnosticsTests
{
    [Fact]
    public void Category_NotFound_ReturnsNotFoundOutcome()
    {
        // arrange
        var exception = new NotFoundException();

        // act
        var category = SubtitleDiagnostics.Category(exception);

        // assert
        category.ShouldBe(TelemetryConstants.NotFoundOutcome);
    }

    [Fact]
    public void Category_Validation_ReturnsValidationFailedOutcome()
    {
        // arrange
        var exception = new ValidationException(
            new Dictionary<string, string[]>(StringComparer.Ordinal) { ["Track"] = ["unavailable"] });

        // act
        var category = SubtitleDiagnostics.Category(exception);

        // assert
        category.ShouldBe(TelemetryConstants.HandlerOutcomeValidationFailed);
    }

    [Fact]
    public void Category_Denied_ReturnsDeniedOutcome()
    {
        // arrange
        var exception = new LibraryFileAccessDeniedException();

        // act
        var category = SubtitleDiagnostics.Category(exception);

        // assert
        category.ShouldBe(TelemetryConstants.DeniedOutcome);
    }

    [Fact]
    public void Category_Unexpected_ReturnsFailedOutcome()
    {
        // arrange
        var exception = new InvalidOperationException("unexpected");

        // act
        var category = SubtitleDiagnostics.Category(exception);

        // assert
        category.ShouldBe(TelemetryConstants.FailedOutcome);
    }

    [Fact]
    public void TagMovie_NullMovieId_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleDiagnostics.TagMovie(null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void TagFailure_NullException_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleDiagnostics.TagFailure(null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Category_NullException_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleDiagnostics.Category(null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }
}
