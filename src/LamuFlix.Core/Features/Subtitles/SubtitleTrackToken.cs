using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>
/// Opaque, non-ordinal track identifiers: an unkeyed SHA-256 over the invariant movie id, a NUL
/// delimiter and the UTF-8 exact file name, encoded as unpadded base64url. The token identifies a
/// sidecar inside one movie only and is never decoded back into a path.
/// </summary>
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
