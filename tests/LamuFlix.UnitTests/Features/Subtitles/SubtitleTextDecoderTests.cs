using System.Text;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleTextDecoderTests
{
    [Fact]
    public void Decode_Utf8AccentedBytes_ReturnsTheText()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes("Ação e coração");

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("Ação e coração");
    }

    [Fact]
    public void Decode_Utf8ByteOrderMark_IsStrippedFromTheResult()
    {
        // arrange
        byte[] payload = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("cue")];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("cue");
    }

    [Fact]
    public void Decode_Windows1252BytesFallBackAfterStrictUtf8Failure_ReturnsAccentedText()
    {
        // arrange
        var payload = CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes("Ação e coração");

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("Ação e coração");
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00 })]
    [InlineData(new byte[] { 0xFE, 0xFF, 0x00, 0x41 })]
    [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 })]
    public void Decode_UnsupportedUtf16OrUtf32ByteOrderMark_ThrowsSanitizedValidation(byte[] payload)
    {
        // act
        var act = () => SubtitleTextDecoder.Decode(payload);

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public void Decode_CarriageReturnNewlines_NormalizesToLineFeed()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes("one\r\ntwo\rthree");

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("one\ntwo\nthree");
    }
}
