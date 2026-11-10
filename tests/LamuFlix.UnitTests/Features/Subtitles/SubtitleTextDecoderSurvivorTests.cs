using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleTextDecoderSurvivorTests
{
    [Fact]
    public void Decode_TwoByteUtf16LeBom_ThrowsSanitizedValidation()
    {
        // arrange
        byte[] payload = [0xFF, 0xFE];

        // act
        var act = () => SubtitleTextDecoder.Decode(payload);

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public void Decode_TwoByteUtf16BeBom_ThrowsSanitizedValidation()
    {
        // arrange
        byte[] payload = [0xFE, 0xFF];

        // act
        var act = () => SubtitleTextDecoder.Decode(payload);

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public void Decode_FirstByteMatchesUtf16LeButSecondDiffers_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0xFF, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ÿ\0");
    }

    [Fact]
    public void Decode_SecondByteMatchesUtf16BeButFirstDiffers_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0x00, 0xFF];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("\0ÿ");
    }

    [Fact]
    public void Decode_FourByteUtf32BeBom_ThrowsSanitizedValidation()
    {
        // arrange
        byte[] payload = [0x00, 0x00, 0xFE, 0xFF];

        // act
        var act = () => SubtitleTextDecoder.Decode(payload);

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public void Decode_FourByteUtf32LeBom_ThrowsSanitizedValidation()
    {
        // arrange
        byte[] payload = [0xFF, 0xFE, 0x00, 0x00];

        // act
        var act = () => SubtitleTextDecoder.Decode(payload);

        // assert
        var thrown = Should.Throw<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public void Decode_AlmostUtf32BeLastByteDiffers_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0x00, 0x00, 0xFE, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("\0\0þ\0");
    }

    [Fact]
    public void Decode_AlmostUtf32LeFirstTwoBytesMatchButRestDiffers_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0xFF, 0x00, 0x00, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ÿ\0\0\0");
    }

    [Fact]
    public void Decode_Utf32LePatternWithNonBomFirstByte_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0x41, 0xFE, 0x00, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("Aþ\0\0");
    }

    [Fact]
    public void Decode_Utf32LePatternWithNonBomSecondByte_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0xFF, 0x41, 0x00, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ÿA\0\0");
    }

    [Fact]
    public void Decode_PartialUtf8BomWithoutThirdByte_DecodesWithoutStripping()
    {
        // arrange
        byte[] payload = [0xEF, 0xBB, 0x00];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ï»\0");
    }

    [Fact]
    public void Decode_BomPrefixShorterThanThreeBytes_DecodesSuccessfully()
    {
        // arrange
        byte[] payload = [0xEF, 0xBB];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ï»");
    }

    [Fact]
    public void Decode_BareUtf8Bom_StripsToEmpty()
    {
        // arrange
        byte[] payload = [0xEF, 0xBB, 0xBF];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Decode_SingleByteInvalidUtf8_FallsBackToWindows1252()
    {
        // arrange
        byte[] payload = [0xE7];

        // act
        var result = SubtitleTextDecoder.Decode(payload);

        // assert
        result.ShouldBe("ç");
    }
}
