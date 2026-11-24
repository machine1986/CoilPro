using System.Net;
using System.Text;

namespace CoilPro.Tests.TestSupport;

/// <summary>
/// Hand-rolled <see cref="HttpMessageHandler"/> stub (no mocking library) that returns a canned
/// response (or a queued sequence of responses, for retry tests) for outgoing requests and records
/// each request so tests can assert on what was actually sent (method, URI, query, auth header, body).
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    /// <summary>All requests sent through this handler, in order.</summary>
    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>The most recently sent request, or null if none has been sent yet.</summary>
    public CapturedRequest? LastRequest => Requests.Count == 0 ? null : Requests[^1];

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

    /// <summary>
    /// Returns each response in <paramref name="responses"/> in order, one per request (for retry
    /// tests: e.g. a 429 followed by a 200). Throws <see cref="InvalidOperationException"/> if more
    /// requests are sent than responses were queued — a test asserting the wrong retry/request count
    /// would surface as this exception instead of failing silently.
    /// </summary>
    public StubHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        _responder = _ =>
        {
            if (queue.Count == 0)
            {
                throw new InvalidOperationException(
                    "StubHttpMessageHandler: no more queued responses — the client sent more requests than the test expected.");
            }

            return queue.Dequeue();
        };
    }

    /// <summary>Convenience factory: a single response with a JSON body.</summary>
    public static StubHttpMessageHandler ReturningJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(JsonResponse(json, statusCode));

    /// <summary>Convenience factory: returns each queued JSON/status pair in order, one per request.</summary>
    public static StubHttpMessageHandler ReturningSequence(params HttpResponseMessage[] responses) =>
        new((IEnumerable<HttpResponseMessage>)responses);

    /// <summary>
    /// Builds a standalone JSON <see cref="HttpResponseMessage"/>, optionally with extra headers
    /// (e.g. <c>x-ratelimit-*</c>, <c>Retry-After</c>). Useful both standalone and as an element of a
    /// <see cref="ReturningSequence"/> queue.
    /// </summary>
    public static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                // Response headers (e.g. x-ratelimit-*, Retry-After) vs. content headers
                // (e.g. Content-Type) live in different collections; try both.
                if (!response.Headers.TryAddWithoutValidation(key, value))
                {
                    response.Content.Headers.TryAddWithoutValidation(key, value);
                }
            }
        }

        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        string? authorizationHeader = request.Headers.Authorization?.ToString();

        Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, body, authorizationHeader));

        return _responder(request);
    }
}

/// <summary>A single request captured by <see cref="StubHttpMessageHandler"/>.</summary>
internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body, string? AuthorizationHeader)
{
    /// <summary>Parses <see cref="Body"/> as <c>application/x-www-form-urlencoded</c> key/value pairs.</summary>
    public IReadOnlyDictionary<string, string> FormValues => UrlEncoded.Parse(Body ?? string.Empty);

    /// <summary>Parses <see cref="Uri"/>'s query string into key/value pairs.</summary>
    public IReadOnlyDictionary<string, string> QueryValues => UrlEncoded.Parse(Uri.Query);
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
