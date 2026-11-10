using System.Collections.Generic;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleWebVttConverterTests
{
    private const string Header = "WEBVTT\n\n";

    [Fact]
    public void Convert_TwoCueDocument_EmitsHeaderTimingAndPayloadInOrder()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,500\nfirst\n\n2\n01:02:03,004 --> 01:02:04,000\nsecond\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldBe(
            $"{Header}00:00:01.000 --> 00:00:02.500\nfirst\n\n01:02:03.004 --> 01:02:04.000\nsecond\n\n");
    }

    [Fact]
    public void Convert_CarriageReturnNewlines_NormalizesToLineFeed()
    {
        // arrange
        const string document = "1\r\n00:00:01,000 --> 00:00:02,000\r\ncue text\r\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldBe($"{Header}00:00:01.000 --> 00:00:02.000\ncue text\n\n");
    }

    [Fact]
    public void Convert_MarkupCharactersInCueText_EscapesEachLiterally()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\n<v Roger> & 5 > 4\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldContain("&lt;v Roger&gt; &amp; 5 &gt; 4");
        result.ShouldNotContain("<v Roger>");
    }

    [Fact]
    public void Convert_NoteStyleAndRegionWordsInsideValidCue_StayLiteral()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:04,000\nNOTE literal\nSTYLE literal\nREGION literal\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldContain("NOTE literal");
        result.ShouldContain("STYLE literal");
        result.ShouldContain("REGION literal");
        result.ShouldNotContain("NOTE\n");
    }

    [Fact]
    public void Convert_OverlappingCues_KeepsBothCues()
    {
        // arrange
        const string document =
            "1\n00:00:01,000 --> 00:00:05,000\nfirst\n\n2\n00:00:02,000 --> 00:00:03,000\nsecond\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldContain("00:00:01.000 --> 00:00:05.000\nfirst");
        result.ShouldContain("00:00:02.000 --> 00:00:03.000\nsecond");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("1")]
    public void Convert_EmptyCueLessOrSequenceOnlyDocument_ThrowsSanitizedValidation(string document)
    {
        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Message.ShouldNotContain("00:00:01");
    }

    [Theory]
    [InlineData("1\n00:00:02,000 --> 00:00:01,000\nreversed")]
    [InlineData("1\n-00:00:01,000 --> 00:00:02,000\nnegative")]
    [InlineData("1\n00:00:01.000 --> 00:00:02.000\nmissing comma milliseconds")]
    [InlineData("1\nnot a timing line\npayload")]
    public void Convert_InvalidTiming_ThrowsSanitizedValidation(string document)
    {
        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        var messages = new System.Collections.Generic.List<string>();
        foreach (var pair in thrown.Errors)
        {
            messages.AddRange(pair.Value);
        }

        string.Join("; ", messages).ShouldNotContain("reversed");
    }

    [Fact]
    public void Convert_ArrowInsideCuePayload_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\nforged --> 00:00:09,000\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_MoreCuesThanConfiguredBound_ThrowsSanitizedValidation()
    {
        // arrange
        const string document =
            "1\n00:00:01,000 --> 00:00:02,000\nfirst\n\n2\n00:00:03,000 --> 00:00:04,000\nsecond\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(maxCues: 1));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_LineLongerThanConfiguredBound_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\ntoo long for the bound\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(maxLineCharacters: 4));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_ExactlyAtLineBound_Succeeds()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\nline\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits(maxLineCharacters: 4));

        // assert
        result.ShouldContain("line");
    }

    [Fact]
    public void Convert_OutputLargerThanConfiguredBound_ThrowsSanitizedValidation()
    {
        // arrange
        const string document = "1\n00:00:01,000 --> 00:00:02,000\npayload\n";

        // act
        var act = () => SubtitleWebVttConverter.Convert(document, Limits(maxOutputBytes: Header.Length));

        // assert
        Should.Throw<ValidationException>(act);
    }

    [Fact]
    public void Convert_NumericIndices_AreDropped()
    {
        // arrange
        const string document = "7\n00:00:01,000 --> 00:00:02,000\npayload\n";

        // act
        var result = SubtitleWebVttConverter.Convert(document, Limits());

        // assert
        result.ShouldBe($"{Header}00:00:01.000 --> 00:00:02.000\npayload\n\n");
        result.ShouldNotContain("7\n");
    }

    private static SubtitleConversionLimits Limits(
        int maxCues = 1000,
        int maxLineCharacters = 1000,
        int maxOutputBytes = 1_000_000) =>
        new(maxCues, maxLineCharacters, maxOutputBytes);
}
