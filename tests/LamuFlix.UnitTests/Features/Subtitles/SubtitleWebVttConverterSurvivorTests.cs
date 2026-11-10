using System;
using System.Text;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleWebVttConverterSurvivorTests
{
    [Fact]
    public void Convert_NullDocument_ThrowsArgumentNullException()
    {
        // arrange
        var limits = Limits(1000);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleWebVttConverter.Convert(null!, limits);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Convert_NullLimits_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleWebVttConverter.Convert("1\n00:00:01,000 --> 00:00:02,000\ncue\n", null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Convert_DocumentWithoutTrailingNewline_EmitsFinalCue()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\ncue";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        result.ShouldBe("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\ncue\n\n");
    }

    [Fact]
    public void Convert_EqualStartAndEndTimestamps_EmitsZeroLengthCue()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:01,000\ncue\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        result.ShouldBe("WEBVTT\n\n00:00:01.000 --> 00:00:01.000\ncue\n\n");
    }

    [Fact]
    public void Convert_ElevenDigitSequenceIndex_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "12345678901\n00:00:01,000 --> 00:00:02,000\ncue\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_NineDigitSequenceIndex_IsAccepted()
    {
        // arrange
        const string document = "123456789\n00:00:01,000 --> 00:00:02,000\ncue\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        result.ShouldBe("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\ncue\n\n");
    }

    [Fact]
    public void Convert_NonDigitSequenceIndex_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1a\n00:00:01,000 --> 00:00:02,000\ncue\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_SixtyMinutes_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:60:00,000 --> 01:00:00,000\ncue\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_SixtySeconds_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:00:60,000 --> 00:01:00,000\ncue\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_FiveDigitMinutes_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:00059:00,000 --> 00:59:01,000\ncue\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(1000));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_FourDigitHours_AreAccepted()
    {
        // arrange
        const string document = "1\n0001:00:00,000 --> 0001:00:01,000\ncue\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(10000));

        // assert
        result.ShouldBe("WEBVTT\n\n01:00:00.000 --> 01:00:01.000\ncue\n\n");
    }

    [Fact]
    public void Convert_MaximumHours_AreAccepted()
    {
        // arrange
        const string document = "1\n9999:00:00,000 --> 9999:00:01,000\ncue\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(10000));

        // assert
        result.ShouldBe("WEBVTT\n\n9999:00:00.000 --> 9999:00:01.000\ncue\n\n");
    }

    [Fact]
    public void Convert_OutputExactlyAtByteBound_Succeeds()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\ncue\n";
        var rendered = SubtitleWebVttConverter.Convert(document, Limits(1000000));
        var exactBytes = Encoding.UTF8.GetByteCount(rendered);

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(exactBytes));

        // assert
        result.ShouldBe(rendered);
    }

    [Fact]
    public void Convert_OutputBeyondByteBound_ThrowsSanitizedValidation()
    {
        // arrange
        const string first = "1\n00:00:01,000 --> 00:00:02,000\nfirst\n";
        const string document = "1\n00:00:01,000 --> 00:00:02,000\nfirst\n\n2\n00:00:03,000 --> 00:00:04,000\nsecond\n";
        var firstBytes = Encoding.UTF8.GetByteCount(SubtitleWebVttConverter.Convert(first, Limits(1000000)));

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(firstBytes + 1));

        // assert
        Should.Throw<ValidationException>(act);
    }

    private static SubtitleConversionLimits Limits(int maxOutputBytes) => new(1000, 1000, maxOutputBytes);
}
