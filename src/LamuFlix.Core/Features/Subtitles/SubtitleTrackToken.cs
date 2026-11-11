using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleTrackToken
{
    private const char Delimiter = '\0';

    public static string For(MovieId movieId, string fileName)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        ArgumentNullException.ThrowIfNull(fileName);

        var framed = string.Concat(movieId.Value.ToString(CultureInfo.InvariantCulture), Delimiter, fileName);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(framed));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
