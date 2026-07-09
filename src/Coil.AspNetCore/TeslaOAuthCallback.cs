namespace Coil.AspNetCore;

/// <summary>
/// The relevant query-string values from an OAuth 2.0 redirect ("callback") request, e.g. the request
/// your minimal-API/MVC endpoint receives at the <see cref="TeslaAuthOptions.RedirectUri"/> path.
/// </summary>
public sealed record TeslaOAuthCallbackResult
{
    /// <summary>The authorization code, present on success.</summary>
    public string? Code { get; init; }

    /// <summary>The state value echoed back by the authorization server; compare against the one you generated.</summary>
    public string? State { get; init; }

    /// <summary>OAuth error code (e.g. <c>access_denied</c>), present on failure.</summary>
    public string? Error { get; init; }

    /// <summary>Human-readable OAuth error description, present on failure.</summary>
    public string? ErrorDescription { get; init; }

    /// <summary>True when an authorization <see cref="Code"/> was returned and no <see cref="Error"/> is set.</summary>
    public bool IsSuccess => Error is null && !string.IsNullOrEmpty(Code);
}

/// <summary>
/// Minimal, framework-agnostic parser for the Tesla OAuth redirect query string. Kept dependency-free
/// (no ASP.NET Core <c>HttpRequest</c> reference) so it can be used from minimal APIs, MVC, or the CLI's
/// own loopback listener alike.
/// </summary>
public static class TeslaOAuthCallback
{
    /// <summary>Parses the OAuth callback query parameters out of a full callback <see cref="Uri"/>.</summary>
    public static TeslaOAuthCallbackResult Parse(Uri callbackUri)
    {
        ArgumentNullException.ThrowIfNull(callbackUri);
        return Parse(callbackUri.Query);
    }

    /// <summary>Parses the OAuth callback query parameters out of a raw query string (with or without a leading <c>?</c>).</summary>
    public static TeslaOAuthCallbackResult Parse(string? query)
    {
        string? code = null, state = null, error = null, errorDescription = null;

        if (!string.IsNullOrEmpty(query))
        {
            var span = query.AsSpan().TrimStart('?');
            foreach (var rawPair in span.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = rawPair.IndexOf('=');
                var key = eq < 0 ? rawPair : rawPair[..eq];
                var value = eq < 0 ? string.Empty : Uri.UnescapeDataString(rawPair[(eq + 1)..].Replace('+', ' '));

                switch (key)
                {
                    case "code": code = value; break;
                    case "state": state = value; break;
                    case "error": error = value; break;
                    case "error_description": errorDescription = value; break;
                }
            }
        }

        return new TeslaOAuthCallbackResult
        {
            Code = code,
            State = state,
            Error = error,
            ErrorDescription = errorDescription,
        };
    }
}
