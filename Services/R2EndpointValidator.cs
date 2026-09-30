using System.Text.RegularExpressions;

namespace BlakBox.Api.Services;

public static class R2EndpointValidator
{
    public static bool TryValidate(string? value, out Uri? endpoint)
    {
        endpoint = null;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps ||
            !parsed.IsDefaultPort ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment) ||
            parsed.AbsolutePath != "/" ||
            !Regex.IsMatch(
                parsed.IdnHost,
                @"^[a-f0-9]{32}\.r2\.cloudflarestorage\.com$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return false;
        }

        endpoint = parsed;
        return true;
    }
}
