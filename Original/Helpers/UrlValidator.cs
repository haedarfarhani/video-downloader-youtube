namespace VideoDownloader.Helpers;

/// <summary>
/// Validates user supplied media URLs before they are handed to yt-dlp.
/// Only absolute HTTP(S) URLs without embedded credentials are accepted.
/// </summary>
public static class UrlValidator
{
    public const int MaxUrlLength = 2048;

    public static bool TryValidate(string? input, out Uri? uri, out string? error)
    {
        uri = null;
        error = null;

        var candidate = input?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            error = "Enter a media URL first.";
            return false;
        }

        if (candidate.Length > MaxUrlLength)
        {
            error = $"The URL is longer than the supported limit of {MaxUrlLength} characters.";
            return false;
        }

        foreach (var character in candidate)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
            {
                error = "The URL contains whitespace or control characters.";
                return false;
            }
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed))
        {
            error = "The URL is not a valid absolute URL.";
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            error = "Only http:// and https:// URLs are supported.";
            return false;
        }

        if (string.IsNullOrEmpty(parsed.Host))
        {
            error = "The URL does not contain a host name.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "URLs that embed credentials are not supported.";
            return false;
        }

        uri = parsed;
        return true;
    }

    public static bool IsValid(string? input) => TryValidate(input, out _, out _);
}
