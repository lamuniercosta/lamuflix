using System;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleExceptionMessageTests
{
    [Fact]
    public void MovieNotFound_DefaultMessage_NamesTheMissingLibraryEntry()
    {
        // act
        var exception = new SubtitleMovieNotFoundException();

        // assert
        exception.Message.ShouldBe("The movie is not present in the library.");
    }

    [Fact]
    public void ProviderNotConfigured_DefaultMessage_NamesTheAcquisitionSettings()
    {
        // act
        var exception = new SubtitleProviderNotConfiguredException();

        // assert
        exception.Message.ShouldBe("The subtitle provider acquisition settings are missing or invalid.");
    }

    [Fact]
    public void ReadOnlyLibrary_DefaultMessage_NamesTheDisabledDownloads()
    {
        // act
        var exception = new SubtitleReadOnlyLibraryException();

        // assert
        exception.Message.ShouldBe("Subtitle downloads are disabled for this library.");
    }

    [Fact]
    public void RequestRejected_DefaultMessage_NamesTheInvalidRequest()
    {
        // act
        var exception = new SubtitleRequestRejectedException();

        // assert
        exception.Message.ShouldBe("The subtitle request is not valid.");
    }

    [Fact]
    public void StaleCandidate_DefaultMessage_NamesTheUnavailableCandidate()
    {
        // act
        var exception = new SubtitleStaleCandidateException();

        // assert
        exception.Message.ShouldBe("The selected subtitle candidate is no longer available.");
    }

    [Fact]
    public void SearchRateLimited_DefaultMessage_NamesTheRateLimitedSearch()
    {
        // act
        var exception = new SubtitleSearchRateLimitedException();

        // assert
        exception.Message.ShouldBe("The subtitle search was rate-limited by the provider.");
        exception.RetryAfter.ShouldBeNull();
    }

    [Fact]
    public void SearchRateLimited_RetryAfter_CarriesTheProviderHint()
    {
        // act
        var exception = new SubtitleSearchRateLimitedException(TimeSpan.FromSeconds(30));

        // assert
        exception.Message.ShouldBe("The subtitle search was rate-limited by the provider.");
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void DownloadQuotaExhausted_DefaultMessage_NamesTheExhaustedQuota()
    {
        // act
        var exception = new SubtitleDownloadQuotaExhaustedException();

        // assert
        exception.Message.ShouldBe("The subtitle provider download quota is exhausted.");
        exception.Quota.ShouldBe(SubtitleQuota.Unknown);
    }

    [Fact]
    public void DownloadQuotaExhausted_ExplicitQuota_CarriesTheRemainingDownloads()
    {
        // act
        var exception = new SubtitleDownloadQuotaExhaustedException(new SubtitleQuota(0, null));

        // assert
        exception.Message.ShouldBe("The subtitle provider download quota is exhausted.");
        exception.Quota.RemainingDownloads.ShouldBe(0);
    }
}
