namespace Coil.Tests.TestSupport;

/// <summary>
/// Hand-rolled <see cref="HttpMessageHandler"/> stub (no mocking library) that returns a canned
/// response for every request and records each outgoing request so tests can assert on what was
/// actually sent (method, URI, body).
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    /// <summary>All requests sent through this handler, in order.</summary>
    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>Always returns <paramref name="response"/> for every request.</summary>
    public StubHttpMessageHandler(HttpResponseMessage response)
        : this(_ => response)
    {
    }

    /// <summary>Computes the response per request via <paramref name="responder"/>.</summary>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Convenience factory: a single 200 OK response with a JSON body.</summary>
    public static StubHttpMessageHandler ReturningJson(string json, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) =>
        new(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, body));

        return _responder(request);
    }
}

/// <summary>A single request captured by <see cref="StubHttpMessageHandler"/>.</summary>
internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body)
{
    /// <summary>Parses <see cref="Body"/> as <c>application/x-www-form-urlencoded</c> key/value pairs.</summary>
    public IReadOnlyDictionary<string, string> FormValues => UrlEncoded.Parse(Body ?? string.Empty);
}

/// <summary>
/// Minimal <c>application/x-www-form-urlencoded</c> / query-string parser used only for test
/// assertions (avoids pulling in a mocking/parsing library).
/// </summary>
internal static class UrlEncoded
{
    public static IReadOnlyDictionary<string, string> Parse(string encoded)
    {
        var result = new Dictionary<string, string>();
        var trimmed = encoded.StartsWith('?') ? encoded[1..] : encoded;

        if (string.IsNullOrEmpty(trimmed))
        {
            return result;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var rawKey = idx >= 0 ? pair[..idx] : pair;
            var rawValue = idx >= 0 ? pair[(idx + 1)..] : string.Empty;
            result[Decode(rawKey)] = Decode(rawValue);
        }

        return result;
    }

    // Both query strings and application/x-www-form-urlencoded bodies produced by .NET
    // (Uri.EscapeDataString / FormUrlEncodedContent) may use '+' for spaces; normalize before
    // percent-decoding.
    private static string Decode(string s) => Uri.UnescapeDataString(s.Replace("+", "%20"));
}
